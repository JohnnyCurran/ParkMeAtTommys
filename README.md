# Inspiration

A friend of mine recently moved into the city where I live. On the way to his new apartment to welcome him he sent me a link to the guest parking page. "Not a problem", I thought to myself, "I'll just need to enter my license plate number and I'll be set." This was not the case.

Upon opening the link at [pass2parkit](https://pass2park.it/verifyvisit) I was greeted with a multi-step nightmare of a process full of multiple forms, confirmation modals, unnecessary mobile scrolling, and parking rules which no person with ill-intent would ever see (The man who is bringing explosives into the parking garage is most certainly not taking the time to ensure they are legally parked).

What should have been a painless 5-second process had been taken over by enterprise software and would have to be repeated every time I wanted to visit my friend. I turned my attention to automating the process:

Pictured: Step 1 of 3 of the parking form.

<img width="375" height="812" src="https://i.imgur.com/SDnvbdb.png">

# How It Works

At a high-level, the Web API waits for a GET request at `/api/parking/{id}`. Once received, it pulls my friend's apartment information, my car's information, and launches an instance of Selenium with Chrome. Chromedriver swiftly fills in the information, deftly clicking 'Yes' on the wholly unnecessary modals, and finally screenshots the parking pass once successfully completed.

After a screenshot is obtained, the Gmail API is invoked in order to send an email from my personal email to my phone number (SMS & MMS messages can be sent from email addresses to phone numbers. [See more](https://resources.voyant.com/en/articles/3107728-sending-emails-to-sms-or-mms).

All of this culminates in an iOS shortcut which makes the GET request for me. Now, when I arrive at my friends apartment, I simply say "Hey Siri, Park Me At Tommy's"  and I receive my parking pass.

An added benefit of this system is that since the permits are delivered to my phone via text, I have a (semi) permanenet record of my valid parking pass. In the event I am towed, I have the permit number and time that my parking was valid. This would be made much more difficult if, without a screenshot or other record, I navigated away from the successful parking confirmation message on my phone.

<br>
<br>

# Example

<img width="375" height="812" src="https://i.imgur.com/mrrICGd.jpg">

<img width="375" height="812" src="https://i.imgur.com/dyfx0W0.jpg">

# Deployment & Hosting

The Web API is hosted on a Raspberry Pi 18.04 (Bionic Beaver). I am using [NoIP](https://noip.com) to forward requests to my IP address without needing to keep track of my dynamic IP.

The following Unit file makes sure my service is always running even in the event of a restart or crash:

```
[Unit]
Description=Parking Web API Service

[Service]
WorkingDirectory=/home/ubuntu/ParkMeAtTommys/WebServer/bin/Release/netcoreapp3.1/
ExecStart=/home/ubuntu/dotnet/dotnet /home/ubuntu/ParkMeAtTommys/WebServer/bin/Release/netcoreapp3.1/WebServer.dll
Restart=always
# Restart service after 10 seconds if the dotnet service crashes:
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=parking-app
User=ubuntu
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
```

The iOS shortcut is an extremely simple GET request:

<img width="375" height="812" src="https://imgur.com/fd0tYe4">
