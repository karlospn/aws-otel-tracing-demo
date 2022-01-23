# Introduction
AWS OTEL is a secure, production-ready, AWS-supported distribution of the OpenTelemetry project.

With AWS Distro for OpenTelemetry, you can instrument your applications just once to send correlated metrics and traces to multiple AWS and Partner monitoring solutions.    

It also collects metadata from your AWS resources and managed services, so you can correlate application performance data with underlying infrastructure data, reducing the mean time to problem resolution.

And this repository contains a practical example about how to use AWS OTEL for tracing when we have a series of distributed .NET apps.

# Content

The repository contains the following applications:

![components-diagram](https://raw.githubusercontent.com/karlospn/aws-otel-tracing-demo/main/docs/components-diagram.png)

- **App1.WebApi** is a .NET6 Web API with 2 endpoints.
    - The ``/http`` endpoint  makes an HTTP request to the **App2** ``/dummy`` endpoint.
    - The ``/publish-message``  endpoint queues a message into an **AWS ActiveMQ Rabbit queue**.

- **App2.RabbitConsumer.Console** is a .NET6 console app. 
  - Dequeues messages from the Rabbit queue and makes a HTTP request to the **App3** ``/s3-to-event`` endpoint with the content of the message.

- **App3.WebApi** is a .NET6 Web API with 2 endpoints.
    - The ``/dummy`` endpoint returns a fixed "Ok" response.
    - The ``/s3-to-event`` endpoint receives a message via HTTP POST, stores it in an **S3 bucket** and afterwards publishes the message as an event into an **AWS SQS queue**.

- **App4.SqsConsumer.HostedService** is a .NET6 Worker Service.
  - A Hosted Service reads the messages from the **AWS SQS queue** and stores it into a **DynamoDb table**.

# AWS Resources

> _This repository has a CDK app that will create all these resources._

This demo uses the following AWS resources:

- A VPC (CIDR Address: _10.30.0.0/16_) with a public subnet.
- An internet facing AmazonMQ RabbitMQ cluster.
- An S3 bucket.
- An SQS Queue.
- A Dynamo table.
- An IAM User with the following managed policies:
    - ``AmazonS3FullAccess``
    - ``AmazonSQSFullAccess``
    - ``AmazonDynamoDBFullAccess``
    - Also uses an inline policy with the following permissions:

```javascript
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Effect": "Allow",
            "Action": [
                "logs:PutLogEvents",
                "logs:CreateLogGroup",
                "logs:CreateLogStream",
                "logs:DescribeLogStreams",
                "logs:DescribeLogGroups",
                "xray:*",
                "ssm:GetParameters"
            ],
            "Resource": "*"
        }
    ]
}
```
This policy is needed by the AWS OTEL Collector.

![aws-resources](https://github.com/karlospn/aws-otel-tracing-demo/blob/main/docs/aws-otel-cdk-stack-resources.png)


# OpenTelemetry .NET Client

The apps are using the following package versions:

```xml
    <PackageReference Include="OpenTelemetry" Version="1.2.0-rc1" />
    <PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.0.0-rc8" />
    <PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.0.0-rc8" />
    <PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.0.0-rc8" />
    <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.2.0-rc1" />
    <PackageReference Include="OpenTelemetry.Contrib.Extensions.AWSXRay" Version="1.1.0" />
    <PackageReference Include="OpenTelemetry.Contrib.Instrumentation.AWS" Version="1.0.1" />
```

# How to run the apps

The repository contains a CDK app that creates the AWS Resources needed to run the demo and it also contains a docker-compose file that starts up the 4 apps and the AWS OTEL collector.

There is **some work** that needs to be done in the docker-compose file before you execute it:

- If you take a look at the compose file you'll see that there are a few values that **MUST** be replaced:
    - ``<ADD-AWS-USER-ACCESS-KEY>``
    - ``<ADD-AWS-USER-SECRET-KEY>``
    - ``<ADD-AMAZONMQ-RABBIT-HOST-ENDPOINT>``
    - ``<ADD-S3-BUCKET-NAME>``
    - ``<ADD-SQS-URI>``

You can find the correct values in the output of the CDK app. Here's an example of how the output of the AWS CDK app looks like:

![cdk-app-output](https://github.com/karlospn/aws-otel-tracing-demo/blob/main/docs/aws-otel-cdk-output.png)

And here's an example of how the docker-compose looks like after replacing the placeholder values:

```yaml
version: '3.4'

networks:
  tracing:
    name: tracing-network
    
services:

  otel:
    image: amazon/aws-otel-collector:latest
    command: --config /config/config.yml
    volumes:
      - ./aws-otel-collector-config:/config
    environment:
      - AWS_ACCESS_KEY_ID=AKIA5S6L5S6L5S6L5S6L
      - AWS_SECRET_ACCESS_KEY=GO7BvT9IBLb4NudL0aGO7BvT9IBLb4NudL0a
      - AWS_REGION=eu-west-1
    ports:
      - 4317:4317
    networks:
      - tracing

  app1:
    build:
      context: ./App1.WebApi
    ports:
      - "5000:80"
    networks:
      - tracing
    depends_on: 
      - otel
      - app3
    environment:
      RABBITMQ__HOST: b-27c79732-ce9d-47dc-8c51-13eec94c267e.mq.eu-west-1.amazonaws.com
      RABBITMQ__USERNAME: specialguest
      RABBITMQ__PASSWORD: P@ssw0rd111!
      APP3ENDPOINT: http://app3/dummy
      OTLP__ENDPOINT: http://otel:4317
      OTEL_RESOURCE_ATTRIBUTES: service.name=App1

  app2:
    stdin_open: true
    tty: true
    build:
      context: ./App2.RabbitConsumer.Console
    networks:
      - tracing
    depends_on: 
      - otel
      - app3
    environment:
      RABBITMQ__HOST: b-27c79732-ce9d-47dc-8c51-13eec94c267e.mq.eu-west-1.amazonaws.com
      RABBITMQ__USERNAME: specialguest
      RABBITMQ__PASSWORD: P@ssw0rd111!
      APP3URIENDPOINT: http://app3
      OTLP__ENDPOINT: http://otel:4317
      OTEL_RESOURCE_ATTRIBUTES: service.name=App2


  app3:
    build:
      context: ./App3.WebApi
    ports:
      - "5001:80"
    networks:
      - tracing
    depends_on: 
      - otel
    environment:
      OTLP__ENDPOINT: http://otel:4317
      OTEL_RESOURCE_ATTRIBUTES: service.name=App3
      S3_BUCKET_NAME: aws-otel-demo-s3-bucket-17988
      SQS__URI: https://sqs.eu-west-1.amazonaws.com/7/aws-otel-demo-sqs-queue
      AWS_ACCESS_KEY_ID: AKIA5S6L5S6L5S6L5S6L
      AWS_SECRET_ACCESS_KEY: GO7BvT9IBLb4NudL0aGO7BvT9IBLb4NudL0a
  
  app4:
    build:
      context: ./App4.SqsConsumer.HostedService
    networks:
      - tracing
    depends_on: 
      - otel
    environment:
      OTLP__ENDPOINT: http://otel:4317
      OTEL_RESOURCE_ATTRIBUTES: service.name=App4
      SQS__URI: https://sqs.eu-west-1.amazonaws.com/7777777/aws-otel-demo-sqs-queue
      AWS_ACCESS_KEY_ID: AKIA5S6L5S6L5S6L5S6L
      AWS_SECRET_ACCESS_KEY: GO7BvT9IBLb4NudL0aGO7BvT9IBLb4NudL0a
```

To summarize, if you want to run this demo you'll need to do the following steps:
- Execute the CDK app on your AWS account.
- Replace the values on the docker-compose.
- Execute ``docker-compose up``.

# Output

Here's the XRay trace output:

![xray-trace-output](https://github.com/karlospn/aws-otel-tracing-demo/blob/main/docs/xray-fulltrace.png)

And the XRay Service Map output:

![xray-servicemap-output](https://github.com/karlospn/aws-otel-tracing-demo/blob/main/docs/xray-servicemap-sqs-noise.png)
