FROM node:22-alpine AS build
WORKDIR /app

COPY Frontend/package*.json ./
RUN npm ci

COPY Frontend/ .
RUN npm run build -- --configuration production

FROM nginx:alpine AS final
COPY --from=build /app/dist/Frontend/browser /usr/share/nginx/html
COPY Frontend/nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
