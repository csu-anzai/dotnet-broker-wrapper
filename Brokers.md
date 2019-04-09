# Testing Broker facade on different brokers

## ActiveMQ Artemis

### ActiveMQ Artemis Our implementation
```
docker run --name broker -eENABLE_JMX=true -eJMX_PORT=1199 -eJMX_RMI_PORT=1198 -eARTEMIS_USERNAME=admin -eARTEMIS_PASSWORD=admin -eTOPICS=sample1,sample2,sample3,sample4 -p61613:61613 -p5672:5672 -p61616:61616 -p8161:8161 -p61623:61623 -d registry.asseco.rs/asseco/digitalo-broker:latest-dev
```
### ActiveMQ 2.6.3
```
docker run --name broker -eARTEMIS_USERNAME=admin -eARTEMIS_PASSWORD=admin -p5672:5672 -d vromero/activemq-artemis:2.6.3-alpine
```
### ActiveMQ 2.1.0
```
docker run --name broker -eENABLE_JMX=true -eJMX_PORT=1199 -eJMX_RMI_PORT=1198 -eARTEMIS_USERNAME=admin -eARTEMIS_PASSWORD=admin -eTOPICS=sample1,sample2,sample3,sample4 -p61613:61613 -p5672:5672 -p61616:61616 -p8161:8161 -p61623:61623 -d vromero/activemq-artemis:2.1.0-alpine
```
## RabbitMQ 
```
docker run --name broker -p5672:5672 -p15672:15672 -p1883:1883 -e RABBITMQ_DEFAULT_USER=admin -e RABBITMQ_DEFAULT_PASS=admin rabbitmq
```
## NATS streaming
```
docker run --name nats -d -p 4222:4222 nats-streaming -store file -dir datastore --hb_interval=1s --hb_timeout=1s --hb_fail_count=2
```