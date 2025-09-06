
# Episode 1 - Develop Azure Compute Solutions (25-30% of exam)


link to [video](https://learn.microsoft.com/en-us/shows/exam-readiness-zone/preparing-for-az-204-01-fy25)


- 1.1 Implement Containerized Solutions
- 1.2 Implement Azure App Service Web Apps
- 1.3 Implement Azure Functions



Things that you or may not be tested on in the exam (not exhaustive, but this is a really good starting part)

1.1

- Create and manage container images for solutions
- Publish an image to Azure Container Registry
- Run containers by using Azure Container Instances
- Creation solutions by using Azure Container Apps


1.2

- Create an Azure App Service Web App
- Configure and implement diagnostics and logging
- Deploy code and containerized solutions
- configure settings including transportation layer security (TLS), api settings, and service connections
- implement auto-scaling
- configure deployment slots

1.3

- Create and configure an Azure Function App
- Implement input and output binding
- Implement Function triggers by-using data operations, timers, and webhooks




## ACR - storage location for containers

### use cases of acr
hold container images for services like azure container instances, azure app service web app, azure functions, azure container apps, azure kubernetes service, azure container jobs, etc...

### Service tiers

- Basic
- Standard
- Premium


### acr tasks
configure and manage container image tasks for your registry via acr tasks

#### types of tasks
- quick task
- automatically triggered tasks
- multi-step task

#### use tasks for things such as 

- pulling container images in from another registry
- patching container images using copa
- rebuilding images on base image updates
- building container image based on trigger from azure devops or github


⚠️ Make sure to know the following ⚠️

- The core characteristics of the acr service tiers
- know which acr task scenario fits with which automation use case 
- understand how acr integrates with azure services for seamless container deployment


## ACI

Basically is a just a nice ui that basicallys handles spinning up a vm for you and adding a container run time to it. Its basically a managed  azure vm that has docker on it. 

#### Concepts
Container Groups - A collection of containers that scheduled on the same host machine. This is the top level resource in aci. 

more info can be found [here](https://learn.microsoft.com/en-us/azure/container-instances/container-instances-container-groups)

#### Features 

- Since each aci app gets its own vm, you get the benefit of hypervisor-level security (unlike aca or aks)
- Fast startup times
- Public Ip Connectivity and DNS name
- Custom Images
- Persistent Storage - use external volumes to mount within a container group. This is basically Azure Files managing the docker volumes.
- Linux and Window Containers
- Co-scheduled Groups
- Virtual network deployment

High level concepts

- Deployment - there are two common ways to deploy a multi-container group: an **Arm Template** or a **Yaml File**. ⚠️ Know how both of these work ⚠️
- Resource Allocation - resource requests allocates cpu, memory, and optionally gpu's to a container group.
- Networking - Container groups share an ip address and a port namespace on that ip address. This makes more sense if you go the link for the container groups
- Storage - Specify external volumes to mount within a container group and map those volumes to specific paths within the container.
- Common scenarios - Multi-container groups are useful in cases where you want to divide a single functional task into a small number of container images.

⚠️ Tip for exam ⚠️

- Understand when to use aci
- aci is perfect for simple, stateless, event driven workloads where you dont need full orchestrations




## Azure Container Apps

- This is basically a fancy ui that just uses an aks cluster in the background. everything in azure container apps must be a container as its going to be deployed on aks (in the background), from the user perspective, you dont have to know or care about the aks cluster, its handled and maintained by microsoft.


- Integrates with dapr and keda natively and allows you to create dapr and keda resources for your container apps. 
This means you can configure your apps to scale based on things such as http traffic, event-driven processing, cpu or memory load, any keda-supported scaler


- Just like AKS, Azure Container Apps will run whatever containers you throw at it such as web apps, apis, background jobs, databases, queues, etc...

- This service is perfect if you want the power of AKS, but don't have the expertise, resources, or time to manage an AKS cluster.


## Azure App Service

- Compromised of Azure App Service Plans and Azure App Service Web Apps

- An App Service Plan defines a set of compute resources for a web app to run. One or more apps can be configured to run on the same computing resources (or in the same app service plan).

### Azure App Service Plans - part 1

#### Pricing Tiers of an App Service Plan

- Free
- Shared
- Basic

- Standard
- Premium
- Isolated


When to use what plan 

- Testing and Learning - Free & Shared
- Need **Dedicated Computing** - Basic
- Also need **Autoscaling** and **Storage** - Standard
- Also need higher performance, Faster Scaling, and Vnet support - Premium
- Also need private environment for maximum security - Isolated


##### Tip
For high demand app service web apps, keep them in their own app service plans to avoid resource contension and boost reliablity.


#### How does my app run and scale?

In the **Free** and **Shared** pricing tiers, an app receives CPU minutes on a shared VM instance and can't scale out. 

On **Non Free and Shared** pricing tiers, an app runs on all of the VM instances configured in the App Service Plan.



⚠️ Tip for exam ⚠️

- Understand the trade-offs between pricing tiers
- Know when to move app service plan from one pricing tier to another


### Azure App Service Web App - part 2

Azure App Service Web App is an **http-based service** for hosting
- web applications
- rest apis, and mobile back-ends


Handles and Supports
- a variety of different programming langauges and frameworks
- deploying via code or container
- automatic ssl
- custom domains
- vnet integrations
- managed identities
- hybrid connections
- advanced diagnostics
- performance monitoring



Built-in auto scale support

- Supports automatic scale out/in
- Supports manual scale up/down

- Depending on the usage of the web app, the resources of the underlying machine that are hosting your web app can be scaled up or down manually.


Continuous integration/deployment support

- Azure Devops
- Github
- Bitbucket
- FTP
- local git repository on dev machine


Deployment Slots

- You can easily add deployment slots to an app service web app


App Service on Linux (Web Apps for Containers)

- Host web apps natively on Linux for supported application stacks. Run custom linux containers. 




You can configure the following via the Azure Portal, Azure Cli, Azure Powershell, various iac solutions, etc...

- **Stack settings** - Java, Dotnet, Nodejs, php, etc...
- **Platform Settings** - 32/64 bit architecture
- **Debugging**
- **Incoming Client Certificates** - Basically just an ssl cert


To secure your azure service web app, ensure you use Transport Layer Security (TLS) certificates (previously known as SSL certificates). These certs encrypt the traffic between the browser and the web app on the server itself. Without this configured, the browser will warn you about the site not being secure everytime you visit it.


⚠️ Tip For Exam ⚠️
- Be familiar with where to configure app settings, how to apply tls or ssl certs for web applications.


Deployment Slots - live environments within azure app service that lets you deploy and test new versions of your application without affecting the production environments. These are basically staging environments. 

- You can deploy to and update these staging environments
- You can swap staging with prod environment and swap back if needed.


⚠️ Tip For Exam ⚠️

Know and be familiar with the following

- staging environments
- slot swapping
- swap deployment slots
- Route traffic in app service



### Deploying Solutions

- Supports **code** and **containerized** solutions.

Automated deployment - Automatically push updates as you make changes

Supports
- Azure Devops Services
- Github
- Bitbucket

Manual Deployment - Gives you more control on when your code is pushed to app service
- Git
- CLI
- Zip Deploy
- FTP/S



### Implement Autoscaling

You can enable autoscaling by navigating to app service plan > settings > scale out (App Service Plan)

- Autoscaling works by adding or removing web servers
- Adding Autoscaling improves reliablity, elasticity, availibility, and fault tolerance
- Isnt the best approach to handling long-term growth
- The number of instances of a service is also a factor.





## Azure Functions


Azure Function Apps - Runtime environment for your serverless functions handling hosting, scaling, and execution.


Each function app includes

- your code in C#, java, nodejs (javascript or typescript), powershell core, python, or a custom handler (rust and go examples in docs, other languages can be implemented as well!)
- a function.json file that defines triggers and bindings

Integrates with azure services such as (not an exhaustive list)
- Cosmos Db
- Eventhub
- Eventgrid
- Service Bus
- on-prem systems (via service bus)
- Azure Storage
- 3rd party tools like twilio for sms


⚠️ Tip For Exam ⚠️

Know how to create an Azure Function in the Azure Portal!


### Concepts

Triggers - defines when and how a function runs. every function must have one trigger.

examples of triggers 

- http trigger
- eventhub trigger
- eventgrid trigger
- timer trigger
- storage account trigger


Bindings - connects your functions to other resources. Lets your functions read from and write to external services without needing to write external integration code.  

Binding Types
- Input
- Output


Example of bindings
- configure an azure storage queue input binding (queue item is passed in via args of function)
- configure an azure sql output binding so that the output of the function is outputted to an azure sql database


The way you configure bindings depends on the language itself that you use. For example, in c#, the output binding is defined using attributes and is defined in the attribute constructor or inferred from the parameter type


⚠️ Tip For Exam ⚠️

- Know the differences between triggers and bindings. They're not the same (hint hint)!
- Know how to configure triggers and bindings in mutliple languages and frameworks. So be familiar with how to do it in 
  - c#
  - java
  - python
  - node.js - javascript
  - node.js - typescript
  - powershell core

