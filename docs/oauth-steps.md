# OAuth Token Refresh Guide

Both Zomato and Swiggy MCP servers use OAuth 2.0 with PKCE (S256). Tokens expire periodically and need to be refreshed.

| Service | Token Lifetime | Scope |
|---------|---------------|-------|
| Zomato  | ~30 days      | `offline openid` |
| Swiggy  | ~5 days       | One token for Food, Instamart, and Dineout |

## Prerequisites

- `python3`, `curl`, `openssl` available (standard on WSL)
- Working directory: `src/WoofAgent.Cli`

## Zomato Token Refresh

### 1. Register a client

```bash
curl -s -X POST https://mcp-server.zomato.com/register \
  -H "Content-Type: application/json" \
  -d '{
    "client_name": "WoofAgent",
    "redirect_uris": ["https://oauth.pstmn.io/v1/callback"],
    "grant_types": ["authorization_code"],
    "response_types": ["code"],
    "token_endpoint_auth_method": "none"
  }'
```

Note the `client_id` from the response.

### 2. Generate PKCE values

```bash
python3 -c "
import secrets, hashlib, base64
v = secrets.token_urlsafe(32)
c = base64.urlsafe_b64encode(hashlib.sha256(v.encode()).digest()).rstrip(b'=').decode()
print(f'VERIFIER={v}')
print(f'CHALLENGE={c}')
"
```

### 3. Open authorization URL in browser

Build the URL (replace `CLIENT_ID`, `CHALLENGE`):

```
https://mcp-server.zomato.com/authorize?response_type=code&client_id=CLIENT_ID&redirect_uri=https%3A%2F%2Foauth.pstmn.io%2Fv1%2Fcallback&code_challenge=CHALLENGE&code_challenge_method=S256&state=RANDOM_STATE&scope=mcp%3Atools
```

Use `python3 -c "import urllib.parse; ..."` to URL-encode properly. The `state` and `redirect_uri` must be URL-encoded.

Log in, authorize, and copy the `code` from the callback URL.

### 4. Exchange code for token

```bash
curl -s -X POST https://mcp-server.zomato.com/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=authorization_code&code=AUTH_CODE&code_verifier=VERIFIER&client_id=CLIENT_ID&redirect_uri=https://oauth.pstmn.io/v1/callback"
```

### 5. Store the token

```bash
cd src/WoofAgent.Cli
dotnet user-secrets set "McpServers:Zomato:AccessToken" "TOKEN_VALUE"
```

## Swiggy Token Refresh

Swiggy requires the client to be named `"Claude"` and does **not** allow Postman callback URLs. Use a localhost redirect with a local callback server.

### 1. Register a client

```bash
curl -s -X POST https://mcp.swiggy.com/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "client_name": "Claude",
    "redirect_uris": ["http://localhost:8765/callback"],
    "grant_types": ["authorization_code"],
    "response_types": ["code"],
    "token_endpoint_auth_method": "none"
  }'
```

The `client_id` is always `swiggy-mcp`.

### 2. Generate PKCE and start local callback server

Save this as a one-shot script or run inline. It generates PKCE, prints the auth URL, starts a local server on port 8765, and automatically exchanges the code for a token when the callback arrives.

```bash
python3 << 'PYEOF'
import secrets, hashlib, base64, urllib.parse, json
from http.server import HTTPServer, BaseHTTPRequestHandler
import urllib.request, threading

# Generate PKCE
verifier = secrets.token_urlsafe(32)
challenge = base64.urlsafe_b64encode(hashlib.sha256(verifier.encode()).digest()).rstrip(b'=').decode()
state = secrets.token_urlsafe(16)

params = urllib.parse.urlencode({
    'response_type': 'code',
    'client_id': 'swiggy-mcp',
    'redirect_uri': 'http://localhost:8765/callback',
    'code_challenge': challenge,
    'code_challenge_method': 'S256',
    'state': state,
})

print(f'\nOpen this URL in your browser:\n')
print(f'https://mcp.swiggy.com/auth/authorize?{params}\n')
print('Waiting for callback on http://localhost:8765 ...\n')

class Handler(BaseHTTPRequestHandler):
    def do_GET(self):
        from urllib.parse import urlparse, parse_qs
        parsed = urlparse(self.path)
        p = parse_qs(parsed.query)
        self.send_response(200)
        self.send_header('Content-Type', 'text/html')
        self.end_headers()
        if 'code' in p:
            code = p['code'][0]
            data = urllib.parse.urlencode({
                'grant_type': 'authorization_code',
                'code': code,
                'code_verifier': verifier,
                'client_id': 'swiggy-mcp',
                'redirect_uri': 'http://localhost:8765/callback',
            }).encode()
            req = urllib.request.Request('https://mcp.swiggy.com/auth/token', data=data,
                headers={'Content-Type': 'application/x-www-form-urlencoded'})
            try:
                with urllib.request.urlopen(req, timeout=10) as resp:
                    result = json.loads(resp.read().decode())
                    token = result['access_token']
                    self.wfile.write(b'<h1>Success! Token obtained. You can close this tab.</h1>')
                    print(f'\nACCESS TOKEN:\n{token}\n')
                    print(f'Run this to store it:')
                    print(f'cd src/WoofAgent.Cli && dotnet user-secrets set "McpServers:Swiggy:AccessToken" "{token}"')
            except Exception as e:
                self.wfile.write(f'<h1>Error: {e}</h1>'.encode())
                print(f'Error: {e}')
        threading.Thread(target=self.server.shutdown).start()
    def log_message(self, *a): pass

HTTPServer(('localhost', 8765), Handler).serve_forever()
PYEOF
```

### 3. Authorize in browser

Open the URL printed by the script, log in to Swiggy, and authorize. The script will automatically exchange the code and print the token.

### 4. Store the token

```bash
cd src/WoofAgent.Cli
dotnet user-secrets set "McpServers:Swiggy:AccessToken" "TOKEN_VALUE"
```

## Verify tokens are working

```bash
echo "exit" | dotnet run --project src/WoofAgent.Cli -- --zomato
echo "exit" | dotnet run --project src/WoofAgent.Cli -- --swiggy
echo "exit" | dotnet run --project src/WoofAgent.Cli -- --swiggy-instamart
echo "exit" | dotnet run --project src/WoofAgent.Cli -- --swiggy-dineout
```

Each should print `Discovered N tools` without errors.

## OAuth discovery endpoints

These return supported scopes, grant types, and endpoints:

```bash
curl -s https://mcp-server.zomato.com/.well-known/oauth-authorization-server | python3 -m json.tool
curl -s https://mcp.swiggy.com/.well-known/oauth-authorization-server | python3 -m json.tool
```
