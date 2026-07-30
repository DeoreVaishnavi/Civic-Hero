# syntax=docker/dockerfile:1
FROM node:22-alpine AS build
WORKDIR /app
COPY frontend/civichero-web/package*.json ./
RUN if [ -f package-lock.json ]; then npm ci --no-audit --no-fund; else npm install --no-audit --no-fund; fi
COPY frontend/civichero-web/ ./
ARG VITE_API_BASE_URL=/api/v1
ARG VITE_RECAPTCHA_SITE_KEY=
ARG VITE_CAPTCHA_DEVELOPMENT_BYPASS=false
ENV VITE_API_BASE_URL=${VITE_API_BASE_URL}
ENV VITE_RECAPTCHA_SITE_KEY=${VITE_RECAPTCHA_SITE_KEY}
ENV VITE_CAPTCHA_DEVELOPMENT_BYPASS=${VITE_CAPTCHA_DEVELOPMENT_BYPASS}
RUN npm run build

FROM nginxinc/nginx-unprivileged:stable-alpine AS final
COPY deployment/nginx/frontend.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD wget -q -O /dev/null http://127.0.0.1:8080/health || exit 1
