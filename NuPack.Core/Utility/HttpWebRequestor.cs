﻿namespace NuPack {
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Net;
    using System.Net.Cache;

    // REVIEW: This class isn't super clean. Maybe this object should be passed around instead
    // of being static
    public static class HttpWebRequestor {
        public static ZipPackage DownloadPackage(Uri uri) {
            return DownloadPackage(uri, useCache: true);
        }

        [SuppressMessage(
            "Microsoft.Reliability",
            "CA2000:Dispose objects before losing scope",
            Justification = "We can't dispose an object if we want to return it.")]
        public static ZipPackage DownloadPackage(Uri uri, bool useCache) {
            byte[] cachedBytes = null;
            return new ZipPackage(() => {
                if (useCache && cachedBytes != null) {
                    return new MemoryStream(cachedBytes);
                }

                using (Stream responseStream = GetResponseStream(uri)) {
                    // ZipPackages require a seekable stream
                    var memoryStream = new MemoryStream();
                    // Copy the stream
                    responseStream.CopyTo(memoryStream);
                    // Move it back to the beginning
                    memoryStream.Seek(0, SeekOrigin.Begin);

                    if (useCache) {
                        // Cache the bytes for this package
                        cachedBytes = memoryStream.ToArray();
                    }
                    return memoryStream;
                }
            });
        }

        public static Stream GetResponseStream(Uri uri) {
            WebRequest request = WebRequest.Create(uri);
            InitializeRequest(request);

            WebResponse response = request.GetResponse();

            return response.GetResponseStream();
        }

        internal static void InitializeRequest(WebRequest request) {
            request.CachePolicy = new HttpRequestCachePolicy();
            request.UseDefaultCredentials = true;
            if (request.Proxy != null) {
                // If we are going through a proxy then just set the default credentials
                request.Proxy.Credentials = CredentialCache.DefaultCredentials;
            }
        }
    }
}
