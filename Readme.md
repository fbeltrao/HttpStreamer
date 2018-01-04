# Http streaming tool that forwards json elements to a publisher
Listen for http streams and forwards message to providers. Currently only Azure Event Hub has been implemented.

# You can run locally on you machine (using docker)

Listening for meetup open events
```cmd
docker run -t -d --env HTTPSTREAM_EVENTHUB="{event hub connection string}" --env HTTPSTREAM_URL="/2/open_events" --env HTTPSTREAM_HOST="stream.meetup.com" fbeltrao/httpstreamer:0.3
```
