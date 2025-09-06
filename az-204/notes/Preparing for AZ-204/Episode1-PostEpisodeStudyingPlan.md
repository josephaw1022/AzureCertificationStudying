# Post Episode Studying

There were a lot concepts touched on in the first episode. And there were a lot things mentioned verbally and things shown in the powerpoint slides. 

So I wanted to consolidate what material that was mentioned but not gone into, material that wasn't mentioned, or anything that's important and needs to be known.


Let's do this by section


### ACR (Azure Container Registry)

- Basics - What ACR does and what its' purpose is

- Memorize the service tiers and know the differences between them

- Memorize types of acr tasks, know how to implement, and know when to use which kind of task.

- How it integrates with other Azure Services

- Take notes from this [learning module](https://learn.microsoft.com/en-us/training/modules/publish-container-image-to-azure-container-registry/)

- create terraform modules of each acr tier and show the feature of each tier

### ACI (Azure Container Instance)

- Basics (what purpose, what is manages for you, etc...)

- Understand when to use ACI

- Understand container groups

- Understand common ways to deploy container groups

- Know when which deployment method is best for which situations

- Take notes from this [learning module] (https://learn.microsoft.com/en-us/training/modules/create-run-container-images-azure-container-instances/)

- Write a Terraform module to create an ACI container group. dont just make basic examples that dont do anything. use things such as vnets, managed identity, azure storage, key vault, etc... 

- write an aci container group using the arm template (make it complex too)
- write an aci container group using yaml file (make it complex too)



### Azure Container Apps

- Basics (same as before)

- When to use Azure Container Apps

- Takes notes from this [learning module] (https://learn.microsoft.com/en-us/training/modules/implement-azure-container-apps/)

- Create a terraform module that shows multiple tiers and configuration setups for azure container apps. Use this as a way to learn the differences between the tiers and how each one works!



### Azure App Service

- Basics of App Service Plans

- Basics of App Service Web Apps

- Know characteristics of each pricing tier for the app service plans

- Know what pricing tier to choose for which scenarios and when to change or upgrade tiers

- take notes from these [4 learning modules](https://learn.microsoft.com/en-us/training/paths/create-azure-app-service-web-apps/)


- Implement Terraform module that deploys multiple azure app service plans and web apps. Learn how to configure all this via terraform. Explore the features of each tier in this. Actually take your time to learn it. Dont rush through it! Just slow down and soak it all in.

- Implement the same thing the terraform does, but do it through the ui.



### Azure Functions

Quick note - I feel like this video didn't talk about azure functions that much and it seemed to skip over a lot of things. so this there will be a lot of extra learning that is done

- Basics of Functions (function app, triggers, bindings, etc...)

- Takes notes from these [learning modules](https://learn.microsoft.com/en-us/training/paths/implement-azure-functions/)

- Memorize hosting tiers and the characteristics of each

- Write examples for multiple functions wtih different trigger types, input bindings and output bindings. 

- Terraform modules of the multiple azure functions that use a different tier. so one for each tier or multiple for each tier. just learn the nuances of each hosting tiers.