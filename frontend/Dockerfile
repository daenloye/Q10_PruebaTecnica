ARG NODE_BASE_IMAGE=node:20-alpine
FROM ${NODE_BASE_IMAGE}
WORKDIR /app
COPY package.json ./
RUN --mount=type=secret,id=docker-ca,target=/tmp/docker-ca.crt,required=false \
    if [ -s /tmp/docker-ca.crt ] && grep -q "BEGIN CERTIFICATE" /tmp/docker-ca.crt; then \
      NODE_EXTRA_CA_CERTS=/tmp/docker-ca.crt npm install; \
    else npm install; fi
COPY . .
ENV HOST=0.0.0.0
EXPOSE 5173
CMD ["npm", "run", "dev", "--", "--host", "0.0.0.0"]
