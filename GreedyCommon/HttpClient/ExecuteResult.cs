using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HttpClient
{
    public class ExecuteResult<T>
    {
        public bool IsOk { get; set; }
        public Exception Exception { get; set; }
        public T Data { get; set; }
        public string Message { get; set; }
        public ExecuteResult()
        {
            IsOk = false;
            Exception = null;
            Data = default(T);
            Message = string.Empty;
        }
        public ExecuteResult(T data)
        {
            IsOk = true;
            Exception = null;
            Data = data;
            Message = string.Empty;
        }
        public ExecuteResult(Exception ex)
        {
            if (ex == null)
                throw new System.ArgumentNullException("ex");
            IsOk = false;
            Exception = ex;
            Data = default(T);
            Message = ex.Message;
        }
    }
}
