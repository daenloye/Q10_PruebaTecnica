ARG NODE_BASE_IMAGE=node:20-alpine
FROM ${NODE_BASE_IMAGE} AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN --mount=type=secret,id=docker-ca,target=/tmp/docker-ca.crt,required=false \
    if [ -s /tmp/docker-ca.crt ] && grep -q "BEGIN CERTIFICATE" /tmp/docker-ca.crt; then \
      NODE_EXTRA_CA_CERTS=/tmp/docker-ca.crt npm ci --no-audit --no-fund; \
    else npm ci --no-audit --no-fund; fi
COPY . .
RUN npm run build

FROM ${NODE_BASE_IMAGE} AS runtime
ENV NODE_ENV=production PORT=5173 BACKEND_API_URL=http://localhost:5000
WORKDIR /app
COPY --from=build /app/dist/tutorflow/browser ./dist/tutorflow/browser
COPY server.mjs ./server.mjs
EXPOSE 5173
CMD ["node", "server.mjs"]
