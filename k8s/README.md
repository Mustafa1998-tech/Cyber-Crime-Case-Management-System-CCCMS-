# CCCMS Kubernetes Manifests (Docker Desktop)

Apply in order:

```powershell
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/
```

Check:

```powershell
kubectl get pods,svc -n cccms
```

Expected local endpoints:
- Frontend: `http://localhost:4200`
- API: `https://localhost:7261/openapi/v1.json`

If `LoadBalancer` is not exposed on localhost in your Docker Desktop setup:

```powershell
kubectl port-forward svc/cccms-frontend 4200:4200 -n cccms
kubectl port-forward svc/cccms-api 7261:7261 -n cccms
```

If GHCR images are private, create an image pull secret and patch deployments with `imagePullSecrets`.
