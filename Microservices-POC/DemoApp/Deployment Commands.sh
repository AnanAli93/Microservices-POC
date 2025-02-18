az login

az acr login --name harshaecommerceregistry

# TO DO: Create the deployment file (deployment.yaml)

# ---------------

cd c:\azure\DemoWebApplicationSolution

docker build -t demowebapp:latest -f ./DemoWebApplication/Dockerfile .

docker tag demowebapp:latest harshaecommerceregistry.azurecr.io/demowebapp:latest

docker push harshaecommerceregistry.azurecr.io/demowebapp:latest

# ---------------

# Attach ACR to the cluster (Grant the AKS managed identity the AcrPull role on the ACR):
    az aks update --resource-group ecommerce-resource-group --name ecommerce-aks-cluster --attach-acr harshaecommerceregistry


# Apply the deployment yaml file:
    cd DemoWebApplication
    kubectl apply -f deployment.yaml


# Get list of deployments in the current namespace:
    kubectl get deployments

#   Lists all deployments in the current namespace.
#   A deployment in Kubernetes is a resource that manages the creation, updating, and scaling of a set of replicas of a pod. It ensures that the desired number of pod replicas are running and provides rolling updates and rollback capabilities to maintain application availability and consistency.


# Get deployment details:
    kubectl describe deployment demowebapp-deployment


# Get list of pods in your cluster:
    kubectl get pods


# Get list of pods of a specific deployment:
    kubectl get pods --selector=app=demowebapp


# Get list of containers in a pod:
    kubectl describe pod demowebapp-deployment-856cff9945-5k6p8


# Get logs of first container of a pod:
    kubectl logs demowebapp-deployment-856cff9945-5k6p8


# Get logs of specific container in a pod:
    kubectl logs demowebapp-deployment-856cff9945-5k6p8 -c demowebapp-container


# Delete a deployment:
    kubectl delete deployment demowebapp-deployment --namespace default


------------

# [Not necessary]

# Add managed-identity on existing cluster:
    az aks update --resource-group demo-resource-group --name ecommerce-aks-cluster --enable-managed-identity

