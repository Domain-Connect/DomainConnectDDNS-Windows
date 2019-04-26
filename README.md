# Domain Connect Dynamic DNS

## Summary ##
Most residential and many small business ISPs do not provide their users with a static IP. Instead, users are assigned a 
temporary IP address.

For most users this is sufficient. However, some users may wish to access servers remotely. This might
be for accessing files, playing a game, routing email, or even running a small on premise web hosting application.

Many routers have built in proprietary protocols for updating a DNS record with the current assigned IP address. But these
are often tied to specific DNS Providers. And there are many bespoke applications that also work with specific providers.

This projects is a Windows Service that monitors your public IP address, and when it changes will update an A record in a
domain with the current IP address.

It does this using Domain Connect, an open source protocol that works with many DNS Providers. As such this service will work
with any Domain Connect DNS Provider that supports the asynchronous protocol and onboards the service template.

Note: This application does not solve the problem of port forwarding for inbound requests.

## How it Works ##

### Setup Program ###

To use the service, it first must be configured. This is done by running the setup program DomainConnectDDNSSetup.exe.

This simple Windows application will ask for a domain name and an optional host name. After entering this information, a 
browser is called to retrieve an access code.

This code is then copied and pasted into the application, and setup is complete.

### The Service ###

There are two ways the background process can run.

One is as a simple Windows Tray application. The other is as a Windows Service.

Both of these work basically the same way. At startup the initial value of DNS is
determined by executing a simple DNS query. At a preset interval, the service 
pings ipify.org to determine the current public facing IP address, 
and if different will update DNS for the host and domain name using Domain Connect.

## Will it work with my Domain (DNS Provider)? ##

The service will work with any Domain whose DNS is running with a Domain Connect DNS Provider that supports the 
asynchronous oAuth based protocol, and who has onboarded the Domain Connect Dynamic DNS Service template.

## Installation ##

There isn't an installer, but the setup is pretty straight forward.

### Executables ###

You can load the project into visual studio and build it, or use the .zip file which contains all the executables and supporting files.  

### Setup ###

Run DomainConnectDDNSSetup.exe. This program will prompt you for a domain name and an optional host. 

After entering, it will determine if your domain supports Domain Connect. If it does, you will be prompted
to authorize the application to access DNS for your domain.

After authorization copy and paste the code into the setup dialog, and click Finish.

### Running the Windows Tray Application ###

To run the Windows System Tray application, you simply need to run the program DomainConnectDDNSUpdateTray.exe. 

Typically you would run this at startup of Windows. How this is done varies by version of Windows. 

One way is to run the Task Manager, select the Startup tab, and add the program DomainConnectDDNSUpdateTray.exe.

### Installing and Running the Service ###

To run as a Windows service, you need to run a cmd prompt as the System Administrator.  Visit the directory
where you unpacked the files, and run the command:

c:\windows\Microsoft.Net\Framework\v4.0.30319\InstallUtil.exe DomainConnectDDNSUpdate.exe

This will install the service instance.

Next set the service to start automatically. This can be done by typing Windows+R and typing in services.msc to run the Service Manager.

From the Service Manager find "Domain Connect DDDNS Update", right click, and select "Properties".

Click Start (to begin the service), and select "Automatic" as the Startup Type to have it start on boot.

### Logging ###

Interesting events are logged to the Windows Event log.

## The Project ###s

The project is a visual studio solution that consists of five projects.

### DomainConnect ###

This is a shared library containing much of the logic of the interaction with 
Domain Connect.

### DomainConnectDDNSSetup ###

This is setup program that is run once. It collects the domain and host, and invokes a web browser to gain OAuth consent. Upon granting, 
the browser is sent to a page which will display the oauth access token.  This should be copied and pasted back into the setup app.

Clicking "Finish" in the setup program will exchange the access code for an oauth token.  This (and all other necessary data) are written to a 
simple configuration file (settings.txt).

### DomainConnectDDNSUpdateTray ###

This is a Windows Tray application that monitors for DNS changes and updates IPs with the host.

### DomainConnectDDNSUpdate ###

This is the Windows Service that monitors and updates the IP with the host.

### Tester ###

This project is rather simplistic, and allows running most of the logic of the program from a command line. This is useful for development and 
for debugging.

## DNS Provider Onboarding ##

This application will work with any DNS Provider that onboards the service template. This can be found at:

https://github.com/Domain-Connect/Templates/blob/master/domainconnect.org.dynamicdns.json

This template uses oAuth, but the use case of this application is fairly unique. 

Many companies support oAuth access. And they support multiple clients. Some of these may be web apps, some client apps. Sometimes registration is
shigh touch, but often self service to the clients and at high scale. But typically it is many clients to one 
oAuth Provider (N:1).

With Domain Connect we are introducing a new concept: the same client working with oAuth against many oAuth providers (1:N). For a 
typical application, an individual client will need to onboard with each and every oAuth provider to maintain the secret.

### Secrets ###

Because this application is a client application (which at this time is fairly unique with Domain Connect), it cannot maintain a secret.
Several considerations were made for how to onboard this application.

#### Implicit ####

The initial oAuth specification indicates that such applications should make the initial request using a mode called "implicit". 

This seems to have fallen out of favor in the oAuth community for good reasons.  Instead, the community now recommends using 
a secretless request for the access token.

#### Secretless ####

Secretless oAuth simply omits the secret from the request for the access token.

To ease the implementation of the DNS Providers, rather than support a secretless implementation it was decided to just make the
secret be known. It will be published in the code, and might as well be published here.

The secret is "inconceivable" (without the quotes).

#### PKCE ####

PKCE is a draft specification that protects the integrity of the redirect url being hijacked from a mobile application. For the 
initial implementation of this application, the web is being used for the redirect target. As such this isn't necessary at this time.

The redirect url for this oauth request

https://dynamicdns.domainconnect.org/ddnscode

### Summary for DNS Provider ###

This application will work with any DNS Provider that onboards the Domain Connect template 

Template: https://github.com/Domain-Connect/Templates/blob/master/domainconnect.org.dynamicdns.json 

Secret: inconceivable

Return URI: https://dynamicdns.domainconnect.org/ddnscode

client_id: domainconnect.org
