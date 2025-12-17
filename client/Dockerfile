FROM node:22-alpine AS build
WORKDIR /app

# Install dependencies with lockfile for reproducibility
COPY package.json package-lock.json ./
RUN npm ci

# Copy source
COPY . .

# Build-time API base URL for Vite (can be overridden via Fly build args)
ARG VITE_API_BASE_URL
ENV VITE_API_BASE_URL=${VITE_API_BASE_URL}

# Build the production bundle
RUN npm run build

FROM nginx:alpine
COPY --from=build /app/dist /usr/share/nginx/html

RUN echo $'\
server { \n\
    listen 8080; \n\
    location / { \n\
        root /usr/share/nginx/html; \n\
        index index.html index.htm; \n\
        try_files $uri $uri/ /index.html; \n\
    } \n\
}' > /etc/nginx/conf.d/default.conf

EXPOSE 8080
CMD ["nginx", "-g", "daemon off;"]