FROM docker.io/library/node:22-alpine

WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci --omit=dev

COPY src ./src
COPY newrelic.cjs ./

EXPOSE 8080

USER node

CMD ["node", "-r", "newrelic", "src/index.js"]
