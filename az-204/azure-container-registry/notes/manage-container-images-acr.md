

# Manage container images in Azure Container Registry


link to the learning module [here](https://learn.microsoft.com/en-us/training/modules/publish-container-image-to-azure-container-registry/)


⚠️ Notes here are just for me to note what seems important and what I should memorize from the learning module ⚠️



Azure Container Registry - store container images using azure managed container registry

Azure Container Registry Tasks - build container images in Azure




You can use the following ci/cd tools too and integrate with Azure Container Registry 

- Azure Pipelines
- Github Actions
- Jenkins

ACR Tasks are just another way to build your container images.

### Types of ACR Tasks

1. quick tasks
2. multi-step tasks
3. auto-triggered tasks


### Service Tiers

**Basic** - A cost optimized service tier for developers learning Azure Container Registry. Basic Registries have the same programattic capabilities as Standard and Premium Tier. However, the the storage and image throughput is going to be less than Standard and Premium


**Standard** - Basic Registry + higher throughput + higher storage capabilities


**Premium** - Standard Registry + even higher throughput and storage + a list of premium features 



### Features shared across all tiers

- Entra Id Authentication Integration (use entra id to log into acr and not rely on acr usernames/passwords or tokens)
- Image Deletion
- Webhooks (make http call to somewhere or something that an image was updated or what not)


### Features only in premium tier

These are features that you typically see large enterprise companies prioritize as important and aren't needed for smaller to medium sized companies. These include

- **Zone Redundancy** - replicates your registry to a minimum of 3 seperate zones in each enabled region. so hosted in at least three different data centers (or zones) in the region so to speak. 
- **Geo-replication** - like a sql server failover instance, but for your container registry instead. So if the us-east datacenters go down, then you can at least fail-over to another datacenter seamlessly and not have a single point of failure.
- **Content Trust** - Image signing and trusting. If youve ever gone to event where people must know the "the password" to get in or use a tab, this is sort of like that. The images must be signed with a key issued by an issuer that is trusted by acr. if only the managed identity used for a pipeline has access to the key needed to sign an image, this prevents someone from pushing malicious code to the registry. You typically would use something like [cosign](https://faun.pub/container-image-signing-with-sigstore-cosign-and-azure-key-vault-eb43c21c5ff9) or [notation](https://learn.microsoft.com/en-us/azure/container-registry/container-registry-tutorial-sign-trusted-ca) 
- **Private links** - You can shutoff public access to your ACR instance and instead have private endpoints configured for acr instance in specific virtual networks. For example, if you have an multi-tenant aks cluster that node pools for running apps, self-hosted ado pipelines, monitoring, etc.. then you could just create a single private link in the vnet created by the cluster to your acr instance and basically lockdown your acr from a network perspective so that literally only the cluster can communicate with it. This does introduce some challenges if trying to debug an issue and need quick to ACR, but with overlay vpn tools, you can easily by-pass those challenges. I know when using geo-replication, you may (I know you have to with sql server and some other services) need to set up a private link for the failover instance too, but I wouldnt worry too much about that.
- **Encrypt Storage with Customer Managed Key** - Use key from azure key vault that is customer managed (you the user of azure) that is used to encrypt the images stored in the acr instance. by using a key from azure key vault, you gain control of encrypting of your storage and now have an auditable record that the storage is secured.

So basically the premium features are more **peace of mind** features for companies

- remove single point of potential failure (for compliance typically)
- security at the build artifact level (for compliance typically)
- security at the network level (for compliance typically)
- security at the data-at-rest level (for compliance typically)


### Supported Image Types

You can store oci compliant images on ACR meaning you can store the following on ACR

- your typical linux container images
- Windows container images
- OCI artifacts (literally any file type can be packaged into an OCI image)
- Helm charts
- Bicep modules
- powershell modules
- etc...




### Note about storing images and repos

Having a high number of repositories and image tags can slowly degregate performance of acr over time. To ensure that performance is slowly dragged down, make sure to delete images that are a certain age and no longer needed. for example, if every time a dev team merges into main, an image is built to acr for 2 or 3 years, removing old images from years ago that are no longer used is going to be helpful. And shoot, if an image is wrongfully removed, you can easily recover it if caught early enough (when soft-delete is enabled), or use your repos and its git history to easily  replicate the build and push of an old image that needs to be recovered.



### Tasks


- **Quick Tasks** - used for one off tasks to quickly build or push an image. think `docker push ...` or `docker build ...` but instead of that running locally on your computer, it runs on Azure instead. So instead you would use `az acr build ...` and this is useful for teams that may not have docker installed locally.

- **Automatically Triggered Tasks** - Enable one or more triggers to build an image 
   - trigger on source code update
   - trigger on base image update
   - trigger on a schedule

- **Multi-step Tasks** - Extend the capability of single image build-and-push capability of acr tasks with multi-step and or multi-container based workflows.


**Note** - Each ACR Task has an associated source code context.

So your task could have one of two contexts (a location of a set of source files used to build a container image or artifact)

- Local filesystem
- Git Repository

This makes a lot more sense by looking at examples at these [az acr build docs](https://learn.microsoft.com/en-us/cli/azure/acr?view=azure-cli-latest#az-acr-build)


Let's go through each of the tasks type



#### Quick Tasks

way to quickly build and or push container image builds an integrated fashion. meaning I can quickly build and push an image to acr without running the image build and pushing of the image on my laptop. for more info look at `az acr build docs` below 

[az acr build docs](https://learn.microsoft.com/en-us/cli/azure/acr?view=azure-cli-latest#az-acr-build)

#### Trigger task on source code update

build the image in acr tasks whenever a merge-in happens in acr. so you give acr a pat to get access to github or ado. it then uses that pat token to keep making api calls to the github or ado and see if a change was pushed. once it sees "change pushed", its going to trigger an webhook and that webhook will trigger the build task for the image.


#### Trigger build on base image update

if the base image of an image stored in acr is updated, then the build task for that image in acr will be triggered.


#### Schedule a task

run acr tasks on a schedule. normally used when using copa to patch container images on a set schedule


#### Multi-step tasks 

whether a quick or source code triggered task, you can use a yaml file to define a series of actions for building and pushing an image into acr. 


example

1. Build web app image
2. run the web app container
3. build a web app test image
4. run the web app test image
5. if the tests pass, build a helm chart archive package
6. perform a helm upgrade using the new helm archive package



To better understand how all of this works, I did this tutorial from the docs seen [here](https://learn.microsoft.com/en-us/azure/container-registry/container-registry-tutorial-quick-task)

there are 5 parts to the tutorial and I did all 5 in this [repo](https://github.com/josephaw1022/acr-build-helloworld-node)


