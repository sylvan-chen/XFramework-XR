namespace XGame.Core
{
    public enum WebRequestStatus
    {
        Success,
        ConnectionError,
        ProtocolError,
        DataProcessingError,
        TimeoutError,
        UnknownError,
    }

    public readonly struct WebDownloadBuffer
    {
        public readonly byte[] Data;
        public readonly string Text;

        public WebDownloadBuffer(byte[] data, string text)
        {
            Data = data;
            Text = text;
        }
    }

    public readonly struct WebRequestResult
    {
        public readonly WebRequestStatus Status;
        public readonly string Error;
        public readonly WebDownloadBuffer DownloadBuffer;

        public WebRequestResult(WebRequestStatus status, string error, WebDownloadBuffer downloadBuffer)
        {
            Status = status;
            Error = error;
            DownloadBuffer = downloadBuffer;
        }
    }
}