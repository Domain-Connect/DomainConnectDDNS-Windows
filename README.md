# Domain Connect Dynamic DNS

== Summary ==
Most residential and many small business ISPs do not provide their users with a static IP. Instead, users are assigned a 
temporary IP address.

For most users this is sufficient. However, some applications may wish to contact servers on premise. This might
be for accessing files, playing a game, routing email, or even running a small on premise web hosting application.

Many routers have built in proprietary protocols for udpating a host name with the current assigned IP address. But these
are often tied to specific DNS Providers.

This projects is a Windows Service that monitors your public IP address, and when it changes will update an A record in a
domain with the current IP address.

This service will work with any Domain Connect DNS Provider that supports the asynchronous protocol and onboards the
service template.

Note: This application does not solve the problem of port forwarding for inbound requests.

== How it works ==

=== Setup ===

To use the service, it first must be configured. This is done by running the setup program DomainConnectDDNSSetup.

This simple Windows application will ask for a domain name and an optional host name. After entering this information, a 
browser is called to retrieve an access code.

This code is then copied and pasted into the application, and setup is complete.

=== The Service ===

The application runs in the background on Windows as a Windows Service. At startup the initial value of DNS is determined 
by executing a simple DNS query. At a preset interval, the service pings ipify.org to determine the current public facing 
IP address, and if different will update DNS for the host and domain name using Domain Connect.

== Will it work with my DNS? ==

The service will work with any Domain Connect DNS Provider that supports the asynchronous oAuth based protocol, and who
has onboarded the Domain Connect Dynamic DNS Service template.

== Installation/Use ==

We haven't created an installer yet. But installation and setup is pretty straight forward. We've packed all the
executable files into a .zip file.

=== Setup ===

Run DomainConnectDDNSSetup.exe. This program will prompt you for a domain name and an optional host. 

After entering, it will determine if your domain supports Domain Connect. If it does, you will be prompted
to authorize the application access to DNS for your domain.

After authorization copy and paste the code into the setup dialog, and click Finish.

=== Installing and Running the Service ===

To install the service, you need to run a cmd prompt as the System Administrator.  Visit the directory
where you unpacked the files, and run the command:

c:\windows\Microsoft.Net\Framework\v4.0.30319\InstallUtil.exe DomainConnectDDNSUpdate.exe

This will install the service instance.

Next set the service to start automatically. This can be done by typing Windows+R and typing in services.msc to run the Service Manager.

From the Service Manager find "Domain Connect DDDNS Update", right click, and select "Properties".

Click Start (to begin the service), and select "Automatic" as the Startup Type to have it start on boot.

=== Logging ===

Interesting events are logged to the Windows Event log.


<<<<<<< HEAD
=======
This project is currently a work in progress.  We are building a dynamic dns windows service that uses domain connect to update an A record in a domain with a DNS Provider.

## Onboarding

This application will work with any DNS Provider that onboards the following template using the asynchronous (oAuth) flow:

https://github.com/Domain-Connect/Templates/blob/master/domainconnect.org.dynamicdns.json

## Unique Application

The use case of this application is fairly unique. 

Most oAuth implementations have many registered clients. Some of these may be web apps, some client apps. Sometimes registration is
high touch, but often self service to the clients and at high scale. But typically it is many clients to one 
oAuth Provider (N:1).

With Domain Connect we are introducing a new concept: the same client working with oAuth against many oAuth providers (N:N). For a 
typical application, an individual client will need to onboard with each and every oAuth provider to maintain the secret.

In this case, we are introducing a client application that wants to onboard with many oAuth Providers. We also want to support
any DNS Provider that properly onboards the template (self-serve). We are completely inverting the typical oAuth onboarding process
(1:N). 

## Secrets

Because this application is a client application (which at this time is seen to be fairly unique), it cannot maintain a secret.
Several considerations were made for how to onboard this application.

### Implicit

The initial oAuth specification indicates that such applications should make the initial request using a mode called "implicit". 

This seems to have fallen out of favor in the oAuth community for good reasons.  Instead, the community now recommends using 
a secretless request for the access token.

### Secretless

Secretless oAuth simply omits the secret from the request for the access token.

To ease the implementation of the DNS Providers, rather than support a secretless implementation it was decided to just make the
secret be known. It will be published in the code, and might as well be published here.

The secret is "inconceivable" (without the quotes).

### PKCE

PKCE is a draft specification that protects the integrity of the redirect url being hijacked from a mobile application. For the 
initial implementation of this application, the web is being used for the redirect target. As such this isn't necessary at this time.

The redirect url for an oauth request

https://dynamicdns.domainconnect.org/ddnscode

## Summary

This application will work with any DNS Provider that onboards the Domain Connect template 

Template: https://github.com/Domain-Connect/Templates/blob/master/domainconnect.org.dynamicdns.json 

Secret: inconceivable

Return URI: https://dynamicdns.domainconnect.org/ddnscode

client_id: domainconnect.org

>>>>>>> d602ace01543baeaba5c4193b820528635cb5de5
