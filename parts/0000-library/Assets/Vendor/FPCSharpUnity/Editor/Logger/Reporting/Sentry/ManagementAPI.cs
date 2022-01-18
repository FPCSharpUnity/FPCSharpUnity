﻿// using System;
// using System.Collections.Generic;
// using System.Collections.Immutable;
// using System.Linq;
// using System.Text;
// using FPCSharpUnity.unity.Extensions;


// using FPCSharpUnity.unity.Functional;
// using FPCSharpUnity.core.exts;
// using FPCSharpUnity.core.functional;
// using FPCSharpUnity.core.json;
// using UnityEditor;
// using UnityEngine;
// using Unit = FPCSharpUnity.core.functional.Unit;
//
// namespace FPCSharpUnity.unity.Logger.Reporting.Sentry {
//   /** An awfully incomplete management API for sentry for using from Editor. **/
//   public static class ManagementAPI {
//     #region Methods
//
//     public static Either<Error, ImmutableList<ClientKey>> listClientKeys(
//       string baseUrl, ProjectData projectData, ApiKey key
//     ) {
//       return request(
//         $"{baseUrl}/api/0/projects/{projectData.organizationSlug}/{projectData.projectSlug}/keys/",
//         Request.get, key, new Dictionary<string, string>()
//       ).mapRight(www =>
//         ((List<object>) Json.Deserialize(www.text)).
//         Select(item => ClientKey.readFromJson((Dictionary<string, object>)item)).
//         ToImmutableList()
//       );
//     }
//
//     public static Either<Error, ClientKey> createClientKey(
//       string baseUrl, ProjectData projectData, ApiKey key, string name
//     ) {
//       var json = Json.Serialize(F.dict(F.t("name", name)));
//       return request(
//         $"{baseUrl}/api/0/projects/{projectData.organizationSlug}/{projectData.projectSlug}/keys/",
//         Request.post(Encoding.UTF8.GetBytes(json)), key, new Dictionary<string, string>()
//       ).mapRight(www => ClientKey.readFromJson(
//         (Dictionary<string, object>) Json.Deserialize(www.text)
//       ));
//     }
//
//     public static Either<Error, Unit> deleteClientKey(
//       string baseUrl, ProjectData projectData, ApiKey key, string keyId
//     ) {
//       return request(
//         $"{baseUrl}/api/0/projects/{projectData.organizationSlug}/{projectData.projectSlug}/keys/{keyId}/",
//         Request.delete, key, new Dictionary<string, string>()
//       ).mapRight(_ => F.unit);
//     }
//
//     public static Either<Error, WWW> request(
//       string url, Request request, ApiKey key,
//       Dictionary<string, string> headers__WillBeMutated,
//       int tryCount = 20
//     ) {
//       if (!request.noBody)
//         headers__WillBeMutated["Content-Type"] = "application/json";
//       headers__WillBeMutated["Authorization"] = key.asAuthHeader;
//       try {
//         WWW www = null;
//         for (var retry = 0; retry < tryCount; retry++) {
//           www = request.www(url, headers__WillBeMutated);
//           while (!www.isDone) {
//             if (EditorUtility.DisplayCancelableProgressBar(
//               $"Fetching WWW (try {retry + 1})", $"{request.method} {url}", www.progress
//             )) {
//               www.Dispose();
//               Log.d.info($"Request to {url} cancelled.");
//               return Either<Error, WWW>.Left(CancelledByUser.instance);
//             }
//           }
//           if (string.IsNullOrEmpty(www.error) || !www.error.ToLower().Contains("timed out")) {
//             break;
//           }
//         }
//         return www.toEither().mapLeft(err => (Error) new NetError(url, err));
//       }
//       finally {
//         EditorUtility.ClearProgressBar();
//       }
//     }
//
//     #endregion
//
//     #region Data
//
//     public interface Error {}
//
//     public class CancelledByUser : Error {
//       public static readonly CancelledByUser instance = new CancelledByUser();
//       CancelledByUser() {}
//
//       public override string ToString() { return "Cancelled by user"; }
//     }
//
//     public class NetError : Error {
//       public readonly string url;
//       public readonly WWWError error;
//
//       public NetError(string url, WWWError error) {
//         this.url = url;
//         this.error = error;
//       }
//
//       public override string ToString() => $"{error} @ '{url}'";
//     }
//
//     public struct ApiKey {
//       public readonly string key, asBase64, asAuthHeader;
//
//       public ApiKey(string key) {
//         this.key = key;
//         asBase64 = $"{key}:".toBase64();
//         asAuthHeader = $"Basic {asBase64}";
//       }
//     }
//
//     public struct Request {
//       public enum HTTPMethod { GET, POST, PUT, DELETE }
//
//       public readonly HTTPMethod method;
//       readonly byte[] body;
//
//       Request(HTTPMethod method, byte[] body) {
//         this.method = method;
//         this.body = body;
//       }
//
//       public bool noBody => method == HTTPMethod.GET || method == HTTPMethod.DELETE;
//
//       public static Request get = new Request(HTTPMethod.GET, null);
//       public static Request delete = new Request(HTTPMethod.DELETE, null);
//       public static Request post(byte[] body) { return new Request(HTTPMethod.POST, body); }
//       public static Request put(byte[] body) { return new Request(HTTPMethod.PUT, body); }
//
//       public WWW www(string url, Dictionary<string, string> headers__WillBeMutated) {
//         switch (method) {
//           case HTTPMethod.GET:
//             return new WWW(url, null, headers__WillBeMutated);
//           case HTTPMethod.POST:
//             return new WWW(url, body, headers__WillBeMutated);
//           case HTTPMethod.DELETE:
//             headers__WillBeMutated["X-HTTP-Method-Override"] = method.ToString();
//             return new WWW(url, null, headers__WillBeMutated);
//           case HTTPMethod.PUT:
//             headers__WillBeMutated["X-HTTP-Method-Override"] = method.ToString();
//             return new WWW(url, body, headers__WillBeMutated);
//           default:
//             throw new IllegalStateException("Unreachable code");
//         }
//       }
//     }
//
//     public struct ProjectData {
//       public readonly string organizationSlug, projectSlug;
//
//       public ProjectData(string organizationSlug, string projectSlug) {
//         this.organizationSlug = organizationSlug;
//         this.projectSlug = projectSlug;
//       }
//     }
//
//     /**
// JSON representation:
//       {
//         "secret": "https://public_part:secret_part@hostname/2",
//         "public": "https://public_part@hostname/2"
//       }
//      **/
//     public struct DSN {
//       public readonly string secretUrl, publicUrl;
//
//       public DSN(string secretUrl, string publicUrl) {
//         this.secretUrl = secretUrl;
//         this.publicUrl = publicUrl;
//       }
//
//       public override string ToString() {
//         return $"{nameof(DSN)}[" +
//                $"{nameof(secretUrl)}: {secretUrl}, " +
//                $"{nameof(publicUrl)}: {publicUrl}" +
//                $"]";
//       }
//
//       public static DSN readFromJson(Dictionary<string, object> json) {
//         return new DSN(secretUrl:(string) json["secret"], publicUrl:(string)json["public"]);
//       }
//     }
//
//     /**
// JSON representation:
// {
//       "dateCreated": "2016-04-06T07:50:18.730Z",
//       "dsn": see DSN,
//       "secret": "secret_part",
//       "id": "string_id",
//       "label": "test",
//       "public": "public_part"
// }
//      **/
//     public struct ClientKey : IEquatable<ClientKey> {
//       public readonly string id, name;
//       public readonly DSN dsn;
//
//       #region Equality
//
//       public bool Equals(ClientKey other) {
//         return string.Equals(id, other.id) && string.Equals(name, other.name) && dsn.Equals(other.dsn);
//       }
//
//       public override bool Equals(object obj) {
//         if (ReferenceEquals(null, obj)) return false;
//         return obj is ClientKey && Equals((ClientKey) obj);
//       }
//
//       public override int GetHashCode() {
//         unchecked {
//           var hashCode = (id != null ? id.GetHashCode() : 0);
//           hashCode = (hashCode * 397) ^ (name != null ? name.GetHashCode() : 0);
//           hashCode = (hashCode * 397) ^ dsn.GetHashCode();
//           return hashCode;
//         }
//       }
//
//       public static bool operator ==(ClientKey left, ClientKey right) { return left.Equals(right); }
//       public static bool operator !=(ClientKey left, ClientKey right) { return !left.Equals(right); }
//
//       #endregion
//
//       public ClientKey(string id, string name, DSN dsn) {
//         this.id = id;
//         this.name = name;
//         this.dsn = dsn;
//       }
//
//       public override string ToString() {
//         return $"{nameof(ClientKey)}[" +
//                $"{nameof(id)}: {id}, " +
//                $"{nameof(name)}: {name}, " +
//                $"{nameof(dsn)}: {dsn}" +
//                $"]";
//       }
//
//       public static ClientKey readFromJson(Dictionary<string, object> json) {
//         var dsn = DSN.readFromJson((Dictionary<string, object>) json["dsn"]);
//         return new ClientKey(
//           id: (string) json["id"],
//           name: (string) json["label"],
//           dsn:dsn
//         );
//       }
//     }
//
// #endregion
//   }
//
//   public static class ErrorExts {
//     public static string asString(this ManagementAPI.Error error) =>
//       $"Sentry operation failed: {error}" +
//       F.opt(error as ManagementAPI.NetError).map(err => $"\n{err.error.www.url}").getOrElse("");
//   }
// }