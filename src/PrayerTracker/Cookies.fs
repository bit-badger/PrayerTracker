module PrayerTracker.Cookies

open Microsoft.AspNetCore.Http
open Newtonsoft.Json
open System
open System.Security.Cryptography
open System.IO

// fsharplint:disable MemberNames

/// Cryptography settings to use for encrypting cookies
type CookieCrypto (key : string, iv : string) =
    
    /// The key for the AES encryptor/decryptor
    member _.Key = Convert.FromBase64String key
    
    /// The initialization vector for the AES encryptor/decryptor
    member _.IV = Convert.FromBase64String iv


/// Helpers for encrypting/decrypting cookies
[<AutoOpen>]
module private Crypto =
  
    /// An instance of the cookie cryptography settings
    let mutable crypto = CookieCrypto ("", "")

    /// Encrypt a cookie payload
    let encrypt (payload : string) =
        use aes = Aes.Create ()
        use enc = aes.CreateEncryptor (crypto.Key, crypto.IV)
        use ms  = new MemoryStream ()
        use cs  = new CryptoStream (ms, enc, CryptoStreamMode.Write)
        use sw  = new StreamWriter (cs)
        sw.Write payload
        sw.Close ()
        (ms.ToArray >> Convert.ToBase64String) ()
    
    /// Decrypt a cookie payload
    let decrypt payload =
        use aes = Aes.Create ()
        use dec = aes.CreateDecryptor (crypto.Key, crypto.IV)
        use ms  = new MemoryStream (Convert.FromBase64String payload)
        use cs  = new CryptoStream (ms, dec, CryptoStreamMode.Read)
        use sr  = new StreamReader (cs)
        sr.ReadToEnd ()

    /// Encrypt a cookie
    let encryptCookie cookie =
        (JsonConvert.SerializeObject >> encrypt) cookie

    /// Decrypt a cookie
    let decryptCookie<'T> payload =
        (decrypt >> JsonConvert.DeserializeObject<'T> >> box) payload
        |> function null -> None | x -> Some (unbox<'T> x)


/// Accessor so that the crypto settings instance can be set during startup
let setCrypto c = Crypto.crypto <- c


/// Properties stored in the Small Group cookie
type GroupCookie =
    {   /// The Id of the small group
        [<JsonProperty "g">]
        GroupId : Guid
        
        /// The password hash of the small group
        [<JsonProperty "p">]
        PasswordHash : string
    }
with
    
    /// Convert these properties to a cookie payload
    member this.toPayload () =
        encryptCookie this
    
    /// Create a set of strongly-typed properties from the cookie payload
    static member fromPayload x =
        try decryptCookie<GroupCookie> x with _ -> None


/// The payload for the timeout cookie
type TimeoutCookie =
    {   /// The Id of the small group to which the user is currently logged in
        [<JsonProperty "g">]
        GroupId : Guid
        
        /// The Id of the user who is currently logged in
        [<JsonProperty "i">]
        Id : Guid
        
        /// The salted timeout hash to ensure that there has been no tampering with the cookie
        [<JsonProperty "p">]
        Password : string
        
        /// How long this cookie is valid
        [<JsonProperty "u">]
        Until : int64
    }
with
    
    /// Convert this set of properties to the cookie payload
    member this.toPayload () =
        encryptCookie this
    
    /// Create a strongly-typed timeout cookie from the cookie payload
    static member fromPayload x =
        try decryptCookie<TimeoutCookie> x with _ -> None


/// The payload for the user's "Remember Me" cookie
type UserCookie =
    {   /// The Id of the group into to which the user is logged
        [< JsonProperty "g">]
        GroupId : Guid
        
        /// The Id of the user
        [<JsonProperty "i">]
        Id : Guid
        
        /// The user's password hash
        [<JsonProperty "p">]
        PasswordHash : string
    }
with
    
    /// Convert this set of properties to a cookie payload
    member this.toPayload () =
        encryptCookie this
    
    /// Create the strongly-typed cookie properties from a cookie payload
    static member fromPayload x =
        try decryptCookie<UserCookie> x with _ -> None


/// Create a salted hash to use to validate the idle timeout key
let saltedTimeoutHash (c : TimeoutCookie) =
    sha1Hash $"Prayer%A{c.Id}Tracker%A{c.GroupId}Idle%d{c.Until}Timeout"

/// Cookie options to push an expiration out by 100 days
let autoRefresh =
    CookieOptions (Expires = Nullable<DateTimeOffset> (DateTimeOffset (DateTime.UtcNow.AddDays 100.)), HttpOnly = true)
