# TLS certificate mount

Production Compose expects these local files, which must never be committed:

- `fullchain.pem`
- `privkey.pem`

Use certificates issued for the hostname stored in `APP_HOST`. For a public deployment,
use an automated certificate-renewal process such as Certbot or terminate TLS at an AWS
load balancer/CloudFront and use the non-TLS Nginx configuration internally.
