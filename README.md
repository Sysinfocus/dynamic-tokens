# Dynamic Tokens (DT)
A .NET minimal api and Blazor projects demonstrating the generation and utility of dynamic tokens.

## Why Dynamic Tokens?
The primary reason for Dynamic Tokens is to protect the API endpoints as much as possible. Due to the dynamic nature of tokens being presented to the server from the client, it is quite impossible for someone to guess the next token and misuse it.

## How does it work?
-  The client logs in using their Username/Password as usual.
-  The server upon validating the user, provides the claims and 25 random tokens to the client back.
-  The client for every next API call, uses its client.token as the Authorization key.
-  The server validates the Authorization key, if valid processes the request else throw returns with status code Unauthorized.
-  For any request, the server needs the same sequence of tokens back else it will revoke and remove the tokens completely and the client needs to login again.
-  The client, on reaching the 25th token, will send a `Refresh Token` request and the process continues.
-  The `Refresh Token` request must be sent on the last token only, else it is deemed as Unauthorized access and the client's authorization is revoked.

## Benefits over JWT
-  JWT, even with the minimum expiry time, has the chance of misuse. DT is as good as JWT+Refresh token with lower payloads.
-  JWT misuse can't be determined hence the challenge for revoking unless reported. DT as it is dynamic, one wrong request, revokes the authorization making it more secure.
-  JWT payloads are comparatively larger then DT.
-  Chances of changing the claims are high in JWT compared to DT as DT is key and one single change will revoke authorization.

## FAQ
1.  Can it be used in multi-deployment environments?
    -  Yes, it can be using Redis as the Distributed Cache to maintain the keys and tokens.
2.  Can anyone misuse the tokens?
    -  Yes, only if they can get to the browser's local storage and copy the usertoken.
3.  What if someone uses the refresh token and hijack the session?
    -  The refresh token will work only for the last available token and it is difficult to guess and one wrong guess, will revoke and the client needs to re-login.
4.  Can the endpoints be protected with Roles?
    -  Yes, using Endpoint filter, the endpoints can be protected and only allow for valid roles.  

---

**Note:**
The Dynamic Tokens are an expirement to make API calls safer and it is not recommended to use in production in any way.
