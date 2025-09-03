using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace XGame.Core
{
    public static class WebRequestHelper
    {
        public static async UniTask<WebRequestResult> WebGetBufferAsync(string uri, float timeout = 60f)
        {
            UnityWebRequest www = UnityWebRequest.Get(uri);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.disposeDownloadHandlerOnDispose = true;

            return await WebRequestInternal(www, timeout);
        }

        public static async UniTask<WebRequestResult> WebGetFileAsync(string uri, string savePath, float timeout = 60f)
        {
            UnityWebRequest www = UnityWebRequest.Get(uri);
            www.downloadHandler = new DownloadHandlerFile(savePath)
            {
                removeFileOnAbort = true
            };
            www.disposeDownloadHandlerOnDispose = true;

            return await WebRequestInternal(www, timeout);
        }

        private static async UniTask<WebRequestResult> WebRequestInternal(UnityWebRequest www, float timeout, bool autoDispose = true)
        {
            var cts = new CancellationTokenSource();
            cts.CancelAfterSlim(TimeSpan.FromSeconds(timeout));
            var (isCanceled, _) = await www.SendWebRequest().WithCancellation(cts.Token).SuppressCancellationThrow();

            WebRequestResult result;
            if (isCanceled)
            {
                result = new WebRequestResult
                (
                    WebRequestStatus.TimeoutError,
                    $"Request for {www.uri} faild. {WebRequestStatus.TimeoutError}: Time out.",
                    default
                );
            }
            else if (www.result == UnityWebRequest.Result.Success)
            {
                result = new WebRequestResult
                (
                    WebRequestStatus.Success,
                    null,
                    new WebDownloadBuffer(www.downloadHandler.data, www.downloadHandler.text)
                );
            }
            else
            {
                WebRequestStatus status;
                switch (www.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                        status = WebRequestStatus.ConnectionError;
                        break;
                    case UnityWebRequest.Result.DataProcessingError:
                        status = WebRequestStatus.DataProcessingError;
                        break;
                    case UnityWebRequest.Result.ProtocolError:
                        status = WebRequestStatus.ProtocolError;
                        break;
                    default:
                        status = WebRequestStatus.UnknownError;
                        break;
                }
                result = new WebRequestResult
                (
                    status,
                    $"Request for {www.uri} failed. {status}: {www.error}",
                    default
                );
            }

            if (autoDispose)
            {
                www.Dispose();
            }

            return result;
        }
    }
}