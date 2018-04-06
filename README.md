# DomainConnectDDNS

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

This application will work with any DNS Provider that onboards the Domain Connect template https://github.com/Domain-Connect/Templates/blob/master/domainconnect.org.dynamicdns.json for an oAuth call using the secret of "xxxx" and a redirect url of https://dynamicdns.domainconnect.org/ddnscode should work.

