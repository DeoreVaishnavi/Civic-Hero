# ------------------------------------------------------------
# Stage 1 - Build React Application
# ------------------------------------------------------------

FROM node:22-alpine AS build

WORKDIR /app

# Copy package files
COPY frontend/civichero-web/package*.json ./

# Install dependencies
RUN npm ci

# Copy application source
COPY frontend/civichero-web/ .

# Build production application
RUN npm run build

# ------------------------------------------------------------
# Stage 2 - Nginx
# ------------------------------------------------------------

FROM nginx:1.27-alpine

RUN rm -rf /usr/share/nginx/html/*

COPY --from=build /app/dist /usr/share/nginx/html

COPY deployment/nginx/nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx","-g","daemon off;"]