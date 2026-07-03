# syntax=docker/dockerfile:1

# ─────────────────────────────────────────────────────────────────────────────
# Stage 1 — deps: install production dependencies only (cached separately)
# ─────────────────────────────────────────────────────────────────────────────
ARG NODE_VERSION=20
FROM node:${NODE_VERSION}-alpine AS deps

WORKDIR /usr/src/app

# Leverage cache mounts so subsequent builds don't re-download packages
RUN --mount=type=bind,source=package.json,target=package.json \
    --mount=type=bind,source=package-lock.json,target=package-lock.json \
    --mount=type=cache,target=/root/.npm \
    npm ci --omit=dev

# ─────────────────────────────────────────────────────────────────────────────
# Stage 2 — build: compile TypeScript and bundle with Vite
# ─────────────────────────────────────────────────────────────────────────────
FROM node:${NODE_VERSION}-alpine AS build

WORKDIR /usr/src/app

# Accept the backend URL at build time so Vite bakes it into the bundle
ARG VITE_API_BASE_URL
ENV VITE_API_BASE_URL=${VITE_API_BASE_URL}

# Install ALL dependencies (including devDependencies needed for tsc + vite)
RUN --mount=type=bind,source=package.json,target=package.json \
    --mount=type=bind,source=package-lock.json,target=package-lock.json \
    --mount=type=cache,target=/root/.npm \
    npm ci

COPY . .
RUN npm run build

# ─────────────────────────────────────────────────────────────────────────────
# Stage 3 — final: serve the static bundle with Nginx
# ─────────────────────────────────────────────────────────────────────────────
FROM nginx:1.27-alpine AS final

# Copy the Vite production build into Nginx's document root
COPY --from=build /usr/src/app/dist /usr/share/nginx/html

# Replace the default Nginx config with our SPA-aware one.
# This overwrites the conf.d/default.conf that ships with the image.
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD wget -qO- http://localhost/health || exit 1

CMD ["nginx", "-g", "daemon off;"]
