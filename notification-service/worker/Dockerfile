FROM docker.io/library/node:22-alpine

WORKDIR /app

COPY package.json package-lock.json ./
RUN npm install --global npm@11.6.2 && npm ci --omit=dev --no-fund

COPY src ./src
COPY newrelic.cjs ./

EXPOSE 8080

USER node

CMD ["node", "-r", "newrelic", "src/index.js"]
