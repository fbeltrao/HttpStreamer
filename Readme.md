# Http streaming tool that forwards json elements to a publisher
Listen for http streams and forwards message to providers. Currently only Azure Event Hub has been implemented.

## Run locally (using docker)

Listening for meetup open events
```cmd
docker run -t -d --env HTTPSTREAM_EVENTHUB="{event hub connection string}" --env HTTPSTREAM_URL="/2/open_events" --env HTTPSTREAM_HOST="stream.meetup.com" fbeltrao/httpstreamer:0.3
```
To see logs:
```cmd
docker logs -t -f {image id}
```

## Run in Azure Container Instance

Running in Azure Container Instance
```cmd
az container create -g <resource group> --name <name> --image fbeltrao/httpstreamer:0.3 --environment-variables HTTPSTREAM_EVENTHUB="<event hub connection string>" HTTPSTREAM_URL="/2/open_events" HTTPSTREAM_HOST="stream.meetup.com" --location <azure location>
```

Checking deployment status
```cmd
az container list -o table
```

Checking instance log
```
az container logs -g <resource group> -n <name>
```
