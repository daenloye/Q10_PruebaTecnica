ARG NODE_BASE_IMAGE=node:20-alpine
FROM ${NODE_BASE_IMAGE}
WORKDIR /tests
COPY runner.mjs ./runner.mjs
CMD ["node", "runner.mjs"]
