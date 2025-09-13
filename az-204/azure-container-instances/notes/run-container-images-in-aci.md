

## Run container images in Azure Container Instances



Simply run container containers without managing vms, storage, or the networking for it. 

- fast startup
- container access
- hypervisor level security
- Can run linux or windows containers.
- custom sizes
- persistent storage 


### Container Groups

The top-level resource in Azure Container Instances is the container group. A container group is a collection of containers that get scheduled on the same host machine. Containers in a container group share a lifecycle, resources, local network, and storage volumes. Very similar to a pod in kubernetes. 



### Deployment

deploy via resource manager template or via a yaml file. A rm template is a recommended when you need to deploy more azure service resourtces when you deploy the container instance. yaml is recommended for when you just want to deploy container instances. 


### Resource Allocation

can specify cpu, memory, and gpu 


### Networking

containers in container group share ip. can integrate into a virtual network 


### Storage

supported volumes

- azure file share 
- secret
- empty directory
- cloned git repo



in order to mount multiple volumes into a container group in aci, you must use arm template or yaml file.

yaml file is preferred approach is just deploying container instances 



