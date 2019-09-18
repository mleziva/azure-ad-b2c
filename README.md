# azure-ad-b2c
Sample application for azure ad b2c custom policies

Demonstrates:
Logging with application insights
Adding to claims via rest API
Multifactor authentication with TOTP


# Getting Started
Create a B2C tenant
Register app in that tenant
Register required identity framework applications in azure ad tenant
Update appsettings.json inside of the SocialAndLocalAccounts folder to have the keys from your local azure ad app registrations
Build the b2c policies using vscode with b2c extension
Publish the B2CSample API and use that URL as the WebAppAPISignUpUrl
Upload the custom policies to azure
Run relying party policies with https://jwt.ms as reply url

Tutorials referenced:
Getting Started: https://docs.microsoft.com/en-us/azure/active-directory-b2c/active-directory-b2c-get-started-custom
Rest API: https://docs.microsoft.com/en-us/azure/active-directory-b2c/active-directory-b2c-custom-rest-api-netfw
App Insights: https://docs.microsoft.com/en-us/azure/active-directory-b2c/active-directory-b2c-troubleshoot-custom
VSCode uploading policies: https://marketplace.visualstudio.com/items?itemName=AzureADB2CTools.aadb2c
Add custom attributes to b2c: https://docs.microsoft.com/en-us/azure/active-directory-b2c/active-directory-b2c-create-custom-attributes-profile-edit-custom
