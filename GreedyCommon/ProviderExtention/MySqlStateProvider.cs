using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Configuration;
using System.Web.SessionState;
using System.Configuration;
using System.Configuration.Provider;
using System.IO;
using BLToolkit.Data;
using BLToolkit.DataAccess;
using BLToolkit.Extensions;
using BLToolkit.Linq;

using BLToolkit.Data.Linq;

using BLToolkit.Mapping;



namespace ProviderExtention
{
    /// <summary>
    /// 基于MongoDB的session状态管理
    /// </summary>
    public class MySqlStateProvider : SessionStateStoreProviderBase
    {
        #region Helper



        private string _providerString = "Mysql";

        private string _cfgString = "SessionDB";



        DbManager GetDbManager()
        {
            return new DbManager(_providerString, _cfgString);
        }


        #endregion

        private SessionStateSection pConfig = null;
        private string connectionString;
        private ConnectionStringSettings pConnectionStringSettings;
        private string eventSource = "mssqlSessionConn";
        private string eventLog = "Application";
        private string exceptionMessage = "An exception occurred. Please contact your administrator.";
        private string pApplicationName;


        //
        // If false, exceptions are thrown to the caller. If true,
        // exceptions are written to the event log.
        //

        private bool pWriteExceptionsToEventLog = false;

        public bool WriteExceptionsToEventLog
        {
            get { return pWriteExceptionsToEventLog; }
            set { pWriteExceptionsToEventLog = value; }
        }


        //
        // The ApplicationName property is used to differentiate sessions
        // in the data source by application.
        //

        public string ApplicationName
        {
            get { return pApplicationName; }
        }


        public override void Initialize(string name, NameValueCollection config)
        {
            //
            // Initialize values from web.config.
            //

            if (config == null)
                throw new ArgumentNullException("config");

            if (name == null || name.Length == 0)
                name = "DbSessionStateStore";

            if (String.IsNullOrEmpty(config["description"]))
            {
                config.Remove("description");
                config.Add("description", "Sample ODBC Session State Store provider");
            }

            // Initialize the abstract base class.
            base.Initialize(name, config);
            pApplicationName = System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath;

            Configuration cfg = WebConfigurationManager.OpenWebConfiguration(ApplicationName);
            pConfig = (SessionStateSection)cfg.GetSection("system.web/sessionState");


            pConnectionStringSettings = ConfigurationManager.ConnectionStrings[config["connName"]];

            if (pConnectionStringSettings == null || pConnectionStringSettings.ConnectionString.Trim() == "")
            {
                throw new ProviderException("Connection string cannot be blank.");
            }

            connectionString = pConnectionStringSettings.ConnectionString;


            DbManager.AddDataProvider(_providerString, new BLToolkit.Data.DataProvider.MySqlDataProvider());

            DbManager.AddConnectionString(
              _providerString,          // Provider name
             _cfgString,       // Configuration
              connectionString); // Connection string 


        }


        //
        // SessionStateStoreProviderBase members
        //

        public override void Dispose()
        {
        }


        //
        // SessionStateProviderBase.SetItemExpireCallback
        //

        public override bool SetItemExpireCallback(SessionStateItemExpireCallback expireCallback)
        {
            return false;
        }


        //
        // SessionStateProviderBase.SetAndReleaseItemExclusive
        //

        public override void SetAndReleaseItemExclusive(HttpContext context,
          string id,
          SessionStateStoreData item,
          object lockId,
          bool newItem)
        {
            // Serialize the SessionStateItemCollection as a string.
            string sessItems = Serialize((SessionStateItemCollection)item.Items);

            using (var ctx = GetDbManager())
            {

                if (newItem)
                {
                    // OdbcCommand to clear an existing expired session if it exists.
                    ctx.SetCommand("DELETE FROM Sessions  WHERE SessionId = @SessionId AND ApplicationName = @ApplicationName  AND Expires < @Expires ",
                    ctx.Parameter("@SessionId", id),
                     ctx.Parameter("@ApplicationName", ApplicationName),
                     ctx.Parameter("@Expires", DateTime.Now)).ExecuteNonQuery();

                    
                    // OdbcCommand to insert the new session item.
                    ctx.SetCommand("INSERT INTO Sessions   (SessionId, ApplicationName, Created, Expires,   LockDate, LockId, Timeout, Locked, SessionItems, Flags) " +
                      " Values(@SessionId ,@ApplicationName, @Created, @Expires,@LockDate, @LockId, @Timeout, @Locked, @SessionItems, @Flags )",
                    ctx.Parameter("@SessionId", id),
                    ctx.Parameter("@ApplicationName", ApplicationName),
                    ctx.Parameter("@Created", DateTime.Now),
                    ctx.Parameter("@Expires", DateTime.Now.AddMinutes((Double)item.Timeout)),
                    ctx.Parameter("@LockDate", DateTime.Now),
                    ctx.Parameter("@LockId", 0),
                    ctx.Parameter("@Timeout", item.Timeout),
                    ctx.Parameter("@Locked", false),
                    ctx.Parameter("@SessionItems", sessItems),
                    ctx.Parameter("@Flags", 0)).ExecuteNonQuery();
                }
                else
                {
                    // OdbcCommand to update the existing session item.
                    ctx.SetCommand(@"UPDATE Sessions SET Expires =@Expires, SessionItems =@SessionItems, Locked =@Locked
                                                            WHERE SessionId =@SessionId  AND ApplicationName =@ApplicationName  AND LockId =@LockId ",
                    ctx.Parameter("@Expires", DateTime.Now.AddMinutes((Double)item.Timeout)),
                    ctx.Parameter("@SessionItems", sessItems),
                   ctx.Parameter("@Locked", false),
                   ctx.Parameter("@SessionId", id),
                    ctx.Parameter("@ApplicationName", ApplicationName),
                    ctx.Parameter("@LockId", lockId)).ExecuteNonQuery();
                }


            }
        }


        //
        // SessionStateProviderBase.GetItem
        //

        public override SessionStateStoreData GetItem(HttpContext context,
          string id,
          out bool locked,
          out TimeSpan lockAge,
          out object lockId,
          out SessionStateActions actionFlags)
        {
            return GetSessionStoreItem(false, context, id, out locked,
              out lockAge, out lockId, out actionFlags);
        }


        //
        // SessionStateProviderBase.GetItemExclusive
        //

        public override SessionStateStoreData GetItemExclusive(HttpContext context,
          string id,
          out bool locked,
          out TimeSpan lockAge,
          out object lockId,
          out SessionStateActions actionFlags)
        {
            return GetSessionStoreItem(true, context, id, out locked,
              out lockAge, out lockId, out actionFlags);
        }


        //
        // GetSessionStoreItem is called by both the GetItem and 
        // GetItemExclusive methods. GetSessionStoreItem retrieves the 
        // session data from the data source. If the lockRecord parameter
        // is true (in the case of GetItemExclusive), then GetSessionStoreItem
        // locks the record and sets a new LockId and LockDate.
        //

        private SessionStateStoreData GetSessionStoreItem(bool lockRecord,
          HttpContext context,
          string id,
          out bool locked,
          out TimeSpan lockAge,
          out object lockId,
          out SessionStateActions actionFlags)
        {
            // Initial values for return value and out parameters.
            SessionStateStoreData item = null;
            lockAge = TimeSpan.Zero;
            lockId = null;
            locked = false;
            actionFlags = 0;
            using (var ctx = GetDbManager())
            {

                // DateTime to check if current session item is expired.
                DateTime expires;
                // String to hold serialized SessionStateItemCollection.
                string serializedItems = "";
                // True if a record is found in the database.
                bool foundRecord = false;
                // True if the returned session item is expired and needs to be deleted.
                bool deleteData = false;
                // Timeout value from the data store.
                int timeout = 0;


                // lockRecord is true when called from GetItemExclusive and
                // false when called from GetItem.
                // Obtain a lock if possible. Ignore the record if it is expired.
                if (lockRecord)
                {
                    var rc = ctx.SetCommand(@"UPDATE Sessions SET  Locked = @Locked1, LockDate =@LockDate 
                        WHERE SessionId =@SessionId  AND ApplicationName = @ApplicationName  AND Locked =@Locked2  AND Expires >@Expires ",
                    ctx.Parameter("@Locked1", true),
                     ctx.Parameter("@LockDate", DateTime.Now),
                     ctx.Parameter("@SessionId", id),
                     ctx.Parameter("@ApplicationName", ApplicationName),
                     ctx.Parameter("@Locked2", false),
                     ctx.Parameter("@Expires", DateTime.Now)).ExecuteNonQuery();

                    if (rc == 0)
                        // No record was updated because the record was locked or not found.
                        locked = true;
                    else
                        // The record was updated.

                        locked = false;
                }

                // Retrieve the current session item information.
                //cmd = new SqlCommand("SELECT Expires, SessionItems, LockId, LockDate, Flags, Timeout  FROM Sessions WHERE SessionId = @SessionId  AND ApplicationName = @ApplicationName", conn);
                //cmd.Parameters.Add("@SessionId", SqlDbType.VarChar, 80).Value = id;
                //cmd.Parameters.Add("@ApplicationName", SqlDbType.VarChar,
                //  255).Value = ApplicationName;

                // Retrieve session item data from the data source.
                var reader = ctx.GetTable<Sessions>().SingleOrDefault(x => x.SessionId == id && x.ApplicationName == ApplicationName);
                if (reader != null)
                {
                    expires = reader.Expires;

                    if (expires < DateTime.Now)
                    {
                        // The record was expired. Mark it as not locked.
                        locked = false;
                        // The session was expired. Mark the data for deletion.
                        deleteData = true;
                    }
                    else
                        foundRecord = true;

                    serializedItems = reader.SessionItems;
                    lockId = reader.LockId;
                    lockAge = DateTime.Now.Subtract(reader.LockDate);
                    actionFlags = (SessionStateActions)reader.Flags;
                    timeout = reader.Timeout;
                }



                // If the returned session item is expired, 
                // delete the record from the data source.
                if (deleteData)
                {
                    ctx.SetCommand("DELETE FROM Sessions  WHERE SessionId =@SessionId  AND ApplicationName =@ApplicationName",
                     ctx.Parameter("@SessionId", id),
                     ctx.Parameter("@ApplicationName", ApplicationName)).ExecuteNonQuery();
                }

                // The record was not found. Ensure that locked is false.
                if (!foundRecord)
                    locked = false;

                // If the record was found and you obtained a lock, then set 
                // the lockId, clear the actionFlags,
                // and create the SessionStateStoreItem to return.
                if (foundRecord && !locked)
                {
                    lockId = (int)lockId + 1;
                    ctx.SetCommand("UPDATE Sessions SET  LockId =@LockId , Flags = 0  WHERE SessionId =@SessionId  AND ApplicationName = @ApplicationName ",
                    ctx.Parameter("@LockId", lockId),
                    ctx.Parameter("@SessionId", id),
                    ctx.Parameter("@ApplicationName", ApplicationName)).ExecuteNonQuery();
                    // If the actionFlags parameter is not InitializeItem, 
                    // deserialize the stored SessionStateItemCollection.
                    if (actionFlags == SessionStateActions.InitializeItem)
                        item = CreateNewStoreData(context, (int)pConfig.Timeout.TotalMinutes);
                    else
                        item = Deserialize(context, serializedItems, timeout);
                }
            }
            return item;
        }




        //
        // Serialize is called by the SetAndReleaseItemExclusive method to 
        // convert the SessionStateItemCollection into a Base64 string to    
        // be stored in an Access Memo field.
        //

        private string Serialize(SessionStateItemCollection items)
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(ms);

            if (items != null)
                items.Serialize(writer);

            writer.Close();

            return Convert.ToBase64String(ms.ToArray());
        }

        //
        // DeSerialize is called by the GetSessionStoreItem method to 
        // convert the Base64 string stored in the Access Memo field to a 
        // SessionStateItemCollection.
        //

        private SessionStateStoreData Deserialize(HttpContext context,
          string serializedItems, int timeout)
        {
            MemoryStream ms =
              new MemoryStream(Convert.FromBase64String(serializedItems));

            SessionStateItemCollection sessionItems =
              new SessionStateItemCollection();

            if (ms.Length > 0)
            {
                BinaryReader reader = new BinaryReader(ms);
                sessionItems = SessionStateItemCollection.Deserialize(reader);
            }

            return new SessionStateStoreData(sessionItems,
              SessionStateUtility.GetSessionStaticObjects(context),
              timeout);
        }

        //
        // SessionStateProviderBase.ReleaseItemExclusive
        //

        public override void ReleaseItemExclusive(HttpContext context,
          string id,
          object lockId)
        {
            using (var ctx = GetDbManager())
            {
                ctx.SetCommand("UPDATE Sessions SET Locked = 0, Expires =@Expires  " +
                   "WHERE SessionId =@SessionId  AND ApplicationName = @ApplicationName  AND LockId =@LockId",
                ctx.Parameter("@Expires", DateTime.Now.AddMinutes(pConfig.Timeout.TotalMinutes)),
                ctx.Parameter("@SessionId", id),
                ctx.Parameter("@ApplicationName", ApplicationName),
                ctx.Parameter("@LockId", lockId)).ExecuteNonQuery();
            }
        }


        //
        // SessionStateProviderBase.RemoveItem
        //

        public override void RemoveItem(HttpContext context,
          string id,
          object lockId,
          SessionStateStoreData item)
        {
            using (var ctx = GetDbManager())
            {
                ctx.SetCommand("DELETE * FROM Sessions  WHERE SessionId = @SessionId  AND ApplicationName =@ApplicationName AND LockId =@LockId",
                ctx.Parameter("@SessionId", id),
                ctx.Parameter("@ApplicationName", ApplicationName),
                ctx.Parameter("@LockId", lockId)).ExecuteNonQuery();

            }

        }



        //
        // SessionStateProviderBase.CreateUninitializedItem
        //

        public override void CreateUninitializedItem(HttpContext context,
          string id,
          int timeout)
        {
            var s = new Sessions()
            {
                ApplicationName = this.ApplicationName,
                Timeout = timeout,
                Created = DateTime.Now,
                Flags = 1,
                Expires = DateTime.Now.AddMinutes((Double)timeout),
                LockDate = DateTime.Now,
                LockId = 0,
                SessionItems = "",
                SessionId = id,
                Locked = false

            };
            using (var ctx = GetDbManager())
            {
                ctx.Insert<Sessions>(s);
            }
            //SqlConnection conn = new SqlConnection(connectionString);
            //SqlCommand cmd = new SqlCommand("INSERT INTO Sessions   (SessionId, ApplicationName, Created, Expires,   LockDate, LockId, Timeout, Locked, SessionItems, Flags) " +
            //  " Values(@SessionId, @ApplicationName, @Created, @Expires,   @LockDate, @LockId, @Timeout, @Locked, @SessionItems, @Flags)", conn);
            //cmd.Parameters.Add("@SessionId", SqlDbType.VarChar, 80).Value = id;
            //cmd.Parameters.Add("@ApplicationName", SqlDbType.VarChar,
            //  255).Value = ApplicationName;
            //cmd.Parameters.Add("@Created", SqlDbType.DateTime).Value
            //  = DateTime.Now;
            //cmd.Parameters.Add("@Expires", SqlDbType.DateTime).Value
            //  = DateTime.Now.AddMinutes((Double)timeout);
            //cmd.Parameters.Add("@LockDate", SqlDbType.DateTime).Value
            //  = DateTime.Now;
            //cmd.Parameters.Add("@LockId", SqlDbType.Int).Value = 0;
            //cmd.Parameters.Add("@Timeout", SqlDbType.Int).Value = timeout;
            //cmd.Parameters.Add("@Locked", SqlDbType.Bit).Value = false;
            //cmd.Parameters.Add("@SessionItems", SqlDbType.VarChar, 0).Value = "";
            //cmd.Parameters.Add("@Flags", SqlDbType.Int).Value = 1;

        }


        //
        // SessionStateProviderBase.CreateNewStoreData
        //

        //public override SessionStateStoreData CreateNewStoreData(
        //  HttpContext context,
        //  double timeout)
        //{

        //    return new SessionStateStoreData(new SessionStateItemCollection(),
        //      SessionStateUtility.GetSessionStaticObjects(context), (int)timeout);
        //}

        public override SessionStateStoreData CreateNewStoreData(HttpContext context, int timeout)
        {
            return new SessionStateStoreData(new SessionStateItemCollection(),
             SessionStateUtility.GetSessionStaticObjects(context), (int)timeout);
        }



        //
        // SessionStateProviderBase.ResetItemTimeout
        //

        public override void ResetItemTimeout(HttpContext context,
                                              string id)
        {
            using (var ctx = GetDbManager())
            {
                ctx.SetCommand("UPDATE Sessions SET Expires =@Expires   WHERE SessionId =@SessionId  AND ApplicationName =@ApplicationName ",
                ctx.Parameter("@Expires", DateTime.Now.AddMinutes(pConfig.Timeout.TotalMinutes)),
               ctx.Parameter("@SessionId", id),
                ctx.Parameter("@ApplicationName", ApplicationName)).ExecuteNonQuery();
            }



        }


        //
        // SessionStateProviderBase.InitializeRequest
        //

        public override void InitializeRequest(HttpContext context)
        {
        }


        //
        // SessionStateProviderBase.EndRequest
        //

        public override void EndRequest(HttpContext context)
        {
        }


        //
        // WriteToEventLog
        // This is a helper function that writes exception detail to the 
        // event log. Exceptions are written to the event log as a security
        // measure to ensure private database details are not returned to 
        // browser. If a method does not return a status or Boolean
        // indicating the action succeeded or failed, the caller also 
        // throws a generic exception.
        //

        private void WriteToEventLog(Exception e, string action)
        {

        }
    }
}
