# PasswordManager

.NET Core cross platform password manager

## Features

* ASP.NET Core Kestrel web server for reading/writing secrets through web API
* Hmac SHA512 for web API
* WebExtension to set credentials in browser pages (tested with Chrome and Firefox)
* WPF client (Windows only)
* SecureString in memory (partially, WIP)
* AES security for storage

Requires generation of a self signed certificate (pfx) to add in the WebServer project root.