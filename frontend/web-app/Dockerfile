# Use official Node.js 20 Alpine image for base
FROM node:20-alpine AS deps

# Install necessary dependencies
RUN apk add --no-cache libc6-compat

# Set the working directory
WORKDIR /app

# Copy the package.json and package-lock.json or equivalent to install dependencies
COPY frontend/web-app/package*.json ./

# Install production dependencies
RUN npm install --omit-dev --force

# Build stage
FROM node:20-alpine AS builder

# Set the working directory
WORKDIR /app

# Copy the dependencies from the 'deps' stage
COPY --from=deps /app/node_modules ./node_modules

# Copy the rest of the app's files into the container
COPY frontend/web-app ./

# Disable telemetry (optional)
ENV NEXT_TELEMETRY_DISABLED 1

# Run the build process
RUN npm run build

# Production stage: Running the app
FROM node:20-alpine AS runner

# Set the working directory
WORKDIR /app

# Set the environment variable for production
ENV NODE_ENV production
ENV NEXT_TELEMETRY_DISABLED 1

# Create user/group for security
RUN addgroup --system --gid 1001 nodejs
RUN adduser --system --uid 1001 nextjs

# Copy necessary build files to the runner image
COPY --from=builder /app/public ./public
COPY --from=builder /app/package.json ./package.json
COPY --from=builder /app/next.config.ts ./next.config.ts

# Copy the built app's standalone server and static files
COPY --from=builder --chown=nextjs:nodejs /app/.next/standalone ./
COPY --from=builder --chown=nextjs:nodejs /app/.next/static ./.next/static

# Switch to the 'nextjs' user for security
USER nextjs

# Expose the port the app will run on
EXPOSE 3000

# Set the port environment variable (optional)
ENV PORT 3000

# Start the app using the Next.js server
CMD ["node", "server.js"]
