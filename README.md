- [Inspiration](#inspiration)
- [How It Works](#how-it-works)
- [Example](#example)
- [Deployment & Hosting](#deployment---hosting)
- [Reverse Engineering Pass2ParkIt's Web App](#reverse-engineering-pass2parkits-web-app)
  * [Other Solutions](#other-solutions)
  * [Initial Analysis](#initial-analysis)
  * [Step 1: Verify Visit](#step-1--verify-visit)
  * [Step 2: Vehicle Details](#step-2--vehicle-details)
  * [Step 3: Parking Rules & Parking Pass](#step-3--parking-rules---parking-pass)
  * [Step 4: Pure Bash Implementation](#step-4--pure-bash-implementation)

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

<img width="375" height="812" src="https://imgur.com/fd0tYe4.png">

# Reverse Engineering Pass2ParkIt's Web App

The combination of my webserver + Selenium webdriver worked well but there were some things about the solution that bothered me. Namely, the solution was relatively heavy. Pulling in Webdriver just to submit a couple forms seemed like overkill. I decided to see if there was anything I could do to make the solution more lightweight.

## Other Solutions

I considered recreating the network logic using a [C# HttpClient](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-3.1). This would likely be a relatively simple extension on my existing ASP.NET Server. It still felt too "heavy." Making a series of network requests shouldn't have to involve the entire .NET runtime.

I also considered using cURL with Python through [PycURL](http://pycurl.io/). Python is much simpler to get stood up and going than C#/.NET. While a lightweight solution, I thought to myself: "If I'm going to use a cURL wrapper, why not just use cURL itself?"

So I decided to implement my solution in pure bash with the help of cURL.

## Initial Analysis

We know from our Webdriver solution the process to obtain a parking pass is as follows:

1. Verify visit: Enter Phone # and Room # of the person who lives at the complex
2. Enter vehicle details: Enter your vehicle's details (license plate, make, model, year, etc)
3. Parking pass: Accept the parking rules & obtain your pass

I began to inspect the network traffic between my browser and the server while going through the parking flow

## Step 1: Verify Visit

The first step is entering Tommy's phone and apartment number and then pressing "Authorize". When we GET the page at [https://pass2park.it/verifyvisit](https://pass2park.it/verifyvisit) the server sets two cookies: XSRF-TOKEN and laravel\_session. The `Authorize` button calls a function called `checkdetails()`.

Most of that function is making sure the form fields are filled in correctly. It then performs a POST request to `guestapp/verify` to validate the information we entered.

```javascript
    $.ajax({
    url : "https://pass2park.it/guestapp/verify",
    type : 'POST',
    data : {"_token":"c47yujXs1sezyc6LRWImDxcINaOlKutTE93h8z3w","rentroll_phone_no":phno,"rentroll_no":aptno},
    dataType:"JSON",
    success : function(data) {    
      console.log(data);  
          if(data.api_status=='success')
          {
            swal({
                title: 'You are at',
                text:data.property_details[0]['Prop_name'],
                html:true,
                showCancelButton: true,
                confirmButtonColor: "#DD6B55",
                confirmButtonText: "Yes",
                cancelButtonText: "No",
                imageUrl: "assets/gappimages/building1.png",
                }, function() {
                  var session_id=data.session_data.session_id;
                  var authentication_token=data.session_data.authentication_token;
                  var tow_company_id=data.property_details[0]['tow_company_id'];
                  var property_id=data.property_details[0]['Prop_id'];
                  var page1_access_token= $('#accesstoken').val();
                  sessionStorage.setItem("page1_access_token",page1_access_token);
                  sessionStorage.setItem("session_id", session_id); 
                  sessionStorage.setItem("authentication_token",authentication_token); 
                  sessionStorage.setItem("tow_company_id", tow_company_id); 
                  sessionStorage.setItem("property_id",property_id); 
                  window.location = "https://pass2park.it/vehicledetails";
                });
              
          }
          else if(data.api_status=='error')
          {
            $('#errblk').css("display","block");
            $('#errmsg').text(data.api_message);
          } 
        }
    });
```

Here we uncover an important bit of information: The `_token` value sent with the request. This token seems to be some sort of identifier tying the information we enter to the corresponding info on the server.

All the information from the JSON response is saved into session storage. We won't need to worry aobut this since we will not have persistent session storage using strictly cURL and Bash.

In order to extract the token, XSRF token, and laravel\_session from our GET request, we'll use the following cURL command:

```bash
curl -c cookiejar https://pass2park.it/verifyvisit | grep 'data :' | sed 's/data : //' | sed 's/},/}/' | sed "s/phno/$PHONENUM/" | sed "s/aptno/$APTNUM/" | jq --raw-output '._token'
```
With the `-c` flag, cURL saves the cookies into a file named `cookiejar`. We look for the data response, clean up the jquery output so `jq` does not complain, and extract the `_token` field.

Now we can replicate the AJAX call with our vehicle's details with our `_token` using cURL:
```bash
curl -c cookiejar -b cookiejar https://pass2park.it/guestapp/verify --data-raw "_token=$TOKEN&rentroll_phone_no=$PHONENUM&rentroll_no=$APTNUM" > sessionData
```

The `-b` flag sends all the cookies stored in `cookiejar` along with our request. We pass our token, phone number, and apartment number in the `--data-raw` flag which automatically tells cURL to make a POST request.

The response from the server is stored in the file `sessionData` which looks a little like (IDs and locations changed):

```javascript
{
  "api_status": "success",
  "api_message": "valid resident's details.",
  "property_details": [
    {
      "Prop_id": 777,
      "Prop_name": "Redacted",
      "Prop_lat": null,
      "Prop_long": null,
      "tow_company_id": 777,
      "Prop_city": "Denver",
      "Prop_state": "CO",
      "Rent_roll_id": 12345
    }
  ],
  "session_data": {
    "api_status": "success",
    "api_message": "session created successfully",
    "session_id": 217394,
    "authentication_token": "wk547z46nrq2lhkodtif8dma3uhz9u"
  }
}
```

The important values here are `session_id` and `authentication_token`. To extract these from the JSON response we will use `jq`:

```bash
SESSIONID=$(cat sessionData | jq --raw-output '.session_data.session_id')
AUTHTOKEN=$(cat sessionData | jq --raw-output '.session_data.authentication_token')
```

## Step 2: Vehicle Details

The next step is filling in our car's information on this form:

<img height="812" width="375" src="https://imgur.com/F2nTrGQ.png" />

It takes the Plate, State, Make, Model, Year, and Color of our vehicle.

Taking a look at the page's source we can see the `Submit` button calls the `savedetails()` method. A closer look at that method reveals the network call made when we hit submit:

```javascript
$.ajax({
  url : "https://pass2park.it/guestapp/savehicledata",
  type : 'POST',
  data : {"_token":"c47yujXs1sezyc6LRWImDxcINaOlKutTE93h8z3w","session_id":session_id,"authentication_token":authentication_token,"veh_lic_plate_no":lic_plate_no,"veh_state":state,"veh_make":make,"veh_model":model,"veh_year":year,"veh_color":color},
  dataType:"JSON",
  success : function(data) {     
      if(data.api_status=='success')
      {
	var page2_access_token= $('#accesstoken').val();
	sessionStorage.setItem("page2_access_token",page2_access_token);
	sessionStorage.removeItem("page1_access_token");
	window.location = "https://pass2park.it/parkingrules/"+session_id;
      }
      else if(data.api_status=='error')
      {
	    alert(data.api_message);
      }      
  }
});
```

It creates a `POST` request passing both tokens, session id, and all of our vehicle information. The same functionality can be recreated with this cURL command:

```bash
curl -c cookiejar -b cookiejar "https://pass2park.it/guestapp/savehicledata" --data-raw "_token=$TOKEN&session_id=$SESSIONID&authentication_token=$AUTHTOKEN&veh_lic_plate_no=$LICPLATE&veh_state=$LICSTATE&veh_make=$MAKE&veh_model=$MODEL&veh_year=$YEAR&veh_color=$COLOR"
```

## Step 3: Parking Rules & Parking Pass

Once our vehicle is registered and we are presented with the `parking rules` page. We are shown what not to do before pressing `I Agree & Accept`

<img width="375" height="812" src="https://imgur.com/DRCKlo3.png"/>

The JS on this page contains a bunch of what seems to be a developer's test code commented out:

<img src="https://imgur.com/Ug5en4H.png"/>

The `Cancel` button warns you that registration info will be removed and even plays a nice little animation to let you believe it's doing something. Looking at the code, however, reveals it simply waits 1 second before displaying a "Registration cancelled" alert. This suggests the registration is not registered at all until you hit `I Agree & Accept`.

```javascript
function cancel()
{
    swal({
	title: "Are you sure ?",
	text: 'Cancel will remove registration info',
	showCancelButton: true,
	closeOnConfirm: false,
	confirmButtonText: "Yes,Cancel!",
	cancelButtonText: "No",
	imageUrl: "https://pass2park.it/assets/gappimages/question.png",
	showLoaderOnConfirm: true,
	}, function () {
	    setTimeout(function () {
		swal({
		    title: 'Cancelled!',
		    text: "Registration cancelled",
		    type: "success"
	    }, function() {
		sessionStorage.clear();
		window.location = "https://pass2park.it/verifyvisit";
	    });
	}, 1000);
    });
}
```

When you press agree, the following AJAX call is made. It POSTs to `/guestapp/parkingpass` with the tokens and session ID. We're then redirected to our valid pass.

```javascript
$.ajax({
    url : "https://pass2park.it/guestapp/parkingpass",
    type : 'POST',
    data : {"_token":"c47yujXs1sezyc6LRWImDxcINaOlKutTE93h8z3w","session_id":session_id,"authentication_token":authentication_token},
    dataType:"JSON",
    success : function(data) {  
	if(data.api_status=='success')
	{
	    sessionStorage.setItem("pass_id",data.pass_id);
	    sessionStorage.setItem("start_date",data.started_at);
	    sessionStorage.setItem("end_date",data.expires_at);
	    sessionStorage.setItem("apt_no",data.rent_roll_no);
	    sessionStorage.setItem("veh_make",data.vehicle_details.veh_make);
	    sessionStorage.setItem("veh_model",data.vehicle_details.veh_model);
	    sessionStorage.setItem("veh_state",data.vehicle_details.veh_state);
	    sessionStorage.setItem("veh_plate_no",data.vehicle_details.veh_plate_no);
	    // $('#start_date').text(data.started_at);
	    // $('#end_date').text(data.expires_at);
	    // $('#issued_date').text(data.started_at);
	    // $('#apt_no').text("Apartment :  "+data.rent_roll_no);
	    // $('#permitno').text("PERMIT: #"+data.pass_id);
	    // $('#vehdetails').text(data.vehicle_details.veh_make+" "+data.vehicle_details.veh_model+"  ("+data.vehicle_details.veh_state+"-"+data.vehicle_details.veh_plate_no+") ");
	    var page3_access_token= $('#accesstoken').val();
	    sessionStorage.setItem("page3_access_token",page3_access_token);
	    sessionStorage.removeItem("page2_access_token");
	    window.location = "https://pass2park.it/parkingpass";
	}
	else if(data.api_status=='error')
	{
	    alert(data.api_message);
	}
    }
});
```

To obtain our parking pass with cURL, all we need to do is hit the same endpoint with our tokens and session ID:

```bash
curl -c cookiejar -b cookiejar "https://pass2park.it/guestapp/parkingpass" --data-raw "_token=$TOKEN&session_id=$SESSIONID&authentication_token=$AUTHTOKEN"
```

A successful response from the server contains our pass's start and end times in addition to returning the vehicle info for which the pass is valid:

```javascript
{
  "api_status": "success",
  "api_message": "vehicle details saved."
}
{
  "api_status": "success",
  "api_message": "parking pass details",
  "pass_id": 321378,
  "rent_roll_no": "777",
  "started_at": "Mon 17-Aug 2020 01:11 PM",
  "expires_at": "Tue 18-Aug 2020 01:11 PM",
  "vehicle_details": {
    "veh_plate_no": "LIC123",
    "veh_state": "AK",
    "veh_make": "Tesla",
    "veh_model": "Model S"
  }
}
```

## Step 4: Pure Bash Implementation

Now we have all the pieces to create our bash script to obtain our parking pass. A minimal example would look like:

```bash
PHONENUM="555-555-5555" # dashes must be included in the phone number
APTNUM=999
LICPLATE="LIC123"
LICSTATE="AK"
MAKE="Tesla"
MODEL="Model S"
YEAR="2020"
COLOR="Black"

echo "Step 1. Obtain _token"
TOKEN=$(curl -c cookiejar https://pass2park.it/verifyvisit | grep 'data :' | sed 's/data : //' | sed 's/},/}/' | sed "s/phno/\"$PHONENUM\"/" | sed "s/aptno/$APTNUM/" | jq --raw-output '._token')

echo "Step 2: POST token, phone#/apt# to /guestapp/verify"
curl -c cookiejar -b cookiejar https://pass2park.it/guestapp/verify --data-raw "_token=$TOKEN&rentroll_phone_no=$PHONENUM&rentroll_no=$APTNUM" > sessionData

echo "Step 3: Get session ID and auth token"
SESSIONID=$(cat sessionData | jq --raw-output '.session_data.session_id')
AUTHTOKEN=$(cat sessionData | jq --raw-output '.session_data.authentication_token')

echo "Step 4: Post vehicle details to /guestapp/savehicledata"
curl -c cookiejar -b cookiejar "https://pass2park.it/guestapp/savehicledata" --data-raw "_token=$TOKEN&session_id=$SESSIONID&authentication_token=$AUTHTOKEN&veh_lic_plate_no=$LICPLATE&veh_state=$LICSTATE&veh_make=$MAKE&veh_model=$MODEL&veh_year=$YEAR&veh_color=$COLOR"

echo "Step 5: Obtain parking pass"
curl -c cookiejar -b cookiejar "https://pass2park.it/guestapp/parkingpass" --data-raw "_token=$TOKEN&session_id=$SESSIONID&authentication_token=$AUTHTOKEN"
```

And with that, we have replaced our C# selenium webdriver solution with 24 lines of Bash.

Check out the script at [parkMe.sh](parkMe.sh)
