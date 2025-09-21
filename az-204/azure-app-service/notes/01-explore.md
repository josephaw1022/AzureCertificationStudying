# Examine Azure App Service

this module will be high level exploration of azure app service

### Features


**autoscaling** - the ability to scale in/out (horizontitally) and or up/down (vertically) is built into azure app service.
**container support** - leverage container images. supports multi-container apps, window containers, and or docker compose.
**ci/cd support** - leverage built-in github, ado, bitbucket, etc.. support with azure app service
**deployment slots** - deploy pre-prod envs 
**app service on linux** - can run web apps that typically require linux to run or a container
**app service enviornment**  - feature for app service that provides a fully isolated and dedicated environment for running azure app services.


### App Service Plans


app service plans are basically plans for how to run the infrastructure of the app service.

each plan defines the following

- os
- region
- number of vm instances
- size of vm instances
- pricing tier


there are 3 categories for the pricing tiers 


**Shared Compute** - Includes free and shared pricing plans. Customers share their apps on a common pool of vms

**Dedicated Compute** - Includes basic, standard, premium v1, premium v2, and premium v3 pricing tier.

**Isolated** - Includes Isolated v1 and Isolated v2. Runs on dedicated azure vms on a dedicated azure virtual network.



### Deploy to App Service



**Automated Deployments**
  
- Azure Devops
- Github
- Bitbucket


**Manual Deployment**

- Git
- CLI
- Zip Deploy
- FTP/S


Use deployment slots
- when using standard tier, use deployment slots whenever possible and always swap slots when deploying to prod

Continously deploy code
- qa and dev envs should go to a staging slot in azure

container images 

- when using images, you first use pipeline to build container image, tag it and all so that it can go to acr, push the image to acr, and then tell the deployment slot to use that image and the tag that was just pushed up. 


sidecar containers 

- each app deployed can get up to 9 sidecar containers. these can be used for monitoring, logging, or a wide variety of other things as well.


### Built-in Authentication and Authorization

- provides out of the box middleware that when enabled, every request will pass through to ensure the reqeust is authorized to go through.

identity providers 

- microsoft entra
- facebook
- google
- x
- any openid connect provider
- github
- apple
- 
