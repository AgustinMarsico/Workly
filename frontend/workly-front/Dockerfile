FROM docker.io/library/node:22-alpine AS build
WORKDIR /app
ARG API_URL=http://localhost:5000
ARG IDENTITY_API_URL=${API_URL}
ARG RESERVATION_API_URL=${API_URL}
ARG ROOMS_API_URL=${API_URL}
ARG AUDIT_API_URL=http://localhost:5151
ENV API_URL=${API_URL}
ENV IDENTITY_API_URL=${IDENTITY_API_URL}
ENV RESERVATION_API_URL=${RESERVATION_API_URL}
ENV ROOMS_API_URL=${ROOMS_API_URL}
ENV AUDIT_API_URL=${AUDIT_API_URL}

COPY package*.json ./
RUN npm install --global npm@11.6.2 && npm ci --no-audit --no-fund

COPY . .
RUN npm run build -- --configuration production

FROM docker.io/library/nginx:stable-alpine
COPY --from=build /app/dist/workly-front/browser /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
