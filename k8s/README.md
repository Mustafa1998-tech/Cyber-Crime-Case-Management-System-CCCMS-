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
- Prometheus: `http://localhost:9090`
- Grafana: `http://localhost:3000`

If `LoadBalancer` is not exposed on localhost in your Docker Desktop setup:

```powershell
kubectl port-forward svc/cccms-frontend 4200:4200 -n cccms
kubectl port-forward svc/cccms-api 7261:7261 -n cccms
kubectl port-forward svc/cccms-prometheus 9090:9090 -n cccms
kubectl port-forward svc/cccms-grafana 3000:3000 -n cccms
```

If a local port is already in use, choose another local port, for example:
`kubectl port-forward svc/cccms-grafana 3001:3000 -n cccms`.

Grafana default login:
- Username: `admin`
- Password: `GrafanaAdmin@2026!`

Prometheus and Grafana resources:
- `k8s/prometheus-configmap.yaml`
- `k8s/prometheus-deployment.yaml`
- `k8s/prometheus-service.yaml`
- `k8s/monitoring-secret.yaml`
- `k8s/grafana-datasource-configmap.yaml`
- `k8s/grafana-deployment.yaml`
- `k8s/grafana-service.yaml`

If GHCR images are private, create an image pull secret and patch deployments with `imagePullSecrets`.
