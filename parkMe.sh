#!/bin/bash

PHONENUM="555-555-5555" # Phone number must include dashes
APTNUM=999

echo "Step 1. Obtain _token"
TOKEN=$(curl -c cookiejar https://pass2park.it/verifyvisit | grep 'data :' | sed 's/data : //' | sed 's/},/}/' | sed "s/phno/\"$PHONENUM\"/" | sed "s/aptno/$APTNUM/" | jq --raw-output '._token')

echo "Step 2: POST token, phone#/apt# to /guestapp/verify"
curl -c cookiejar -b cookiejar https://pass2park.it/guestapp/verify --data-raw "_token=$TOKEN&rentroll_phone_no=$PHONENUM&rentroll_no=$APTNUM" > sessionData

echo "Step 3: Get session ID and auth token"
SESSIONID=$(cat sessionData | jq --raw-output '.session_data.session_id')
AUTHTOKEN=$(cat sessionData | jq --raw-output '.session_data.authentication_token')
LICPLATE="LIC123"
LICSTATE="AK"
MAKE="Tesla"
MODEL="Model S"
YEAR="2020"
COLOR="Black"

echo "Step 4: Post vehicle details to /guestapp/savehicledata"
curl -c cookiejar -b cookiejar "https://pass2park.it/guestapp/savehicledata" --data-raw "_token=$TOKEN&session_id=$SESSIONID&authentication_token=$AUTHTOKEN&veh_lic_plate_no=$LICPLATE&veh_state=$LICSTATE&veh_make=$MAKE&veh_model=$MODEL&veh_year=$YEAR&veh_color=$COLOR"

echo "Step 5: Obtain parking pass"
curl -c cookiejar -b cookiejar "https://pass2park.it/guestapp/parkingpass" --data-raw "_token=$TOKEN&session_id=$SESSIONID&authentication_token=$AUTHTOKEN"
