const http = require("http");
const fs = require("fs");
const path = require("path");
const url = require("url");

const PORT = process.env.PORT || 6005;

// Mock API responses
const mockResponses = {
  "/api/products": {
    products: [
      {
        id: 1,
        name: "Toy Car",
        price: 25.99,
        category: "Vehicles",
        image: "/images/toy-car.jpg",
      },
      {
        id: 2,
        name: "Puzzle Game",
        price: 15.5,
        category: "Games",
        image: "/images/puzzle.jpg",
      },
      {
        id: 3,
        name: "Action Figure",
        price: 35.0,
        category: "Action",
        image: "/images/action-figure.jpg",
      },
    ],
  },
  "/api/basket": {
    userName: "admin@toyshop.com",
    items: [],
    totalPrice: 0,
  },
  "/api/user": {
    userName: "admin@toyshop.com",
    email: "admin@toyshop.com",
    firstName: "Admin",
    lastName: "User",
    isAuthenticated: true,
  },
};

const server = http.createServer((req, res) => {
  const parsedUrl = url.parse(req.url, true);
  let pathname = parsedUrl.pathname;

  console.log(`Request: ${req.method} ${pathname}`);

  // Set CORS headers
  res.setHeader("Access-Control-Allow-Origin", "*");
  res.setHeader(
    "Access-Control-Allow-Methods",
    "GET, POST, PUT, DELETE, OPTIONS",
  );
  res.setHeader("Access-Control-Allow-Headers", "Content-Type, Authorization");

  // Handle preflight requests
  if (req.method === "OPTIONS") {
    res.writeHead(200);
    res.end();
    return;
  }

  // Handle API routes
  if (pathname.startsWith("/api/")) {
    handleApiRequest(pathname, req, res);
    return;
  }

  // Handle root path
  if (pathname === "/") {
    serveShoppingHomePage(res);
    return;
  }

  // Handle login path
  if (pathname === "/login" || pathname === "/Login") {
    serveLoginRedirect(res);
    return;
  }

  // Handle other shopping app routes
  if (
    pathname.startsWith("/Product") ||
    pathname.startsWith("/Cart") ||
    pathname.startsWith("/Order")
  ) {
    serveShoppingPage(pathname, res);
    return;
  }

  // Default response
  serve404(res);
});

function handleApiRequest(pathname, req, res) {
  if (mockResponses[pathname]) {
    res.writeHead(200, { "Content-Type": "application/json" });
    res.end(JSON.stringify(mockResponses[pathname]));
  } else {
    res.writeHead(404, { "Content-Type": "application/json" });
    res.end(JSON.stringify({ error: "API endpoint not found" }));
  }
}

function serveLoginRedirect(res) {
  // Simulate successful login by redirecting to home
  res.writeHead(302, { Location: "/" });
  res.end();
}

function serveShoppingHomePage(res) {
  const html = `
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>ToyShop - Development Mode</title>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css" rel="stylesheet">
    <link href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.0.0/css/all.min.css" rel="stylesheet">
    <style>
        .hero-section {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 80px 0;
            margin-bottom: 40px;
        }
        .product-card {
            transition: transform 0.3s ease;
            border: none;
            box-shadow: 0 4px 6px rgba(0,0,0,0.1);
        }
        .product-card:hover {
            transform: translateY(-5px);
            box-shadow: 0 8px 25px rgba(0,0,0,0.15);
        }
        .dev-badge {
            position: fixed;
            top: 20px;
            right: 20px;
            background: #28a745;
            color: white;
            padding: 8px 16px;
            border-radius: 20px;
            font-size: 0.8rem;
            z-index: 1000;
        }
        .btn-hero {
            background: rgba(255,255,255,0.2);
            border: 2px solid white;
            color: white;
            padding: 12px 30px;
            border-radius: 25px;
            text-decoration: none;
            display: inline-block;
            transition: all 0.3s ease;
        }
        .btn-hero:hover {
            background: white;
            color: #667eea;
        }
    </style>
</head>
<body>
    <div class="dev-badge">
        <i class="fas fa-code"></i> Development Mode
    </div>

    <!-- Navigation -->
    <nav class="navbar navbar-expand-lg navbar-dark bg-dark">
        <div class="container">
            <a class="navbar-brand" href="#">
                <i class="fas fa-store me-2"></i>ToyShop
            </a>
            <div class="navbar-nav ms-auto">
                <a class="nav-link" href="#products">Products</a>
                <a class="nav-link" href="#cart">
                    <i class="fas fa-shopping-cart me-1"></i>Cart (0)
                </a>
                <a class="nav-link" href="#profile">
                    <i class="fas fa-user me-1"></i>admin@toyshop.com
                </a>
            </div>
        </div>
    </nav>

    <!-- Hero Section -->
    <div class="hero-section">
        <div class="container text-center">
            <h1 class="display-4 mb-4">
                <i class="fas fa-toys me-3"></i>Welcome to ToyShop
            </h1>
            <p class="lead mb-4">
                Development environment for EShop Microservices
            </p>
            <a href="#products" class="btn-hero me-3">
                <i class="fas fa-shopping-bag me-2"></i>Shop Now
            </a>
            <div class="mt-4">
                <small class="text-light">
                    <i class="fas fa-info-circle me-1"></i>
                    Running in mock mode - Identity Server bypass enabled
                </small>
            </div>
        </div>
    </div>

    <!-- Products Section -->
    <div class="container" id="products">
        <h2 class="text-center mb-5">Featured Products</h2>
        <div class="row" id="product-list">
            <!-- Products will be loaded here -->
        </div>
    </div>

    <!-- Status Section -->
    <div class="container mt-5">
        <div class="row">
            <div class="col-12">
                <div class="alert alert-info">
                    <h5><i class="fas fa-info-circle me-2"></i>Development Status</h5>
                    <ul class="mb-0">
                        <li><strong>Shopping.Web:</strong> Running on port ${PORT} (Mock Mode)</li>
                        <li><strong>Authentication:</strong> Bypassed for development</li>
                        <li><strong>APIs:</strong> Mock responses enabled</li>
                        <li><strong>User:</strong> admin@toyshop.com (Mock User)</li>
                    </ul>
                </div>
            </div>
        </div>
    </div>

    <!-- Microservices Status -->
    <div class="container mt-4 mb-5">
        <h3>Microservices Status</h3>
        <div class="row">
            <div class="col-md-4 mb-3">
                <div class="card">
                    <div class="card-body">
                        <h6 class="card-title">
                            <i class="fas fa-circle text-warning me-2"></i>Identity Server
                        </h6>
                        <small class="text-muted">Port 6006 - Not running (bypassed)</small>
                    </div>
                </div>
            </div>
            <div class="col-md-4 mb-3">
                <div class="card">
                    <div class="card-body">
                        <h6 class="card-title">
                            <i class="fas fa-circle text-warning me-2"></i>API Gateway
                        </h6>
                        <small class="text-muted">Port 6004 - Not running (mocked)</small>
                    </div>
                </div>
            </div>
            <div class="col-md-4 mb-3">
                <div class="card">
                    <div class="card-body">
                        <h6 class="card-title">
                            <i class="fas fa-circle text-success me-2"></i>Shopping Web
                        </h6>
                        <small class="text-muted">Port ${PORT} - Running (Mock)</small>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/js/bootstrap.bundle.min.js"></script>
    <script>
        // Load mock products
        fetch('/api/products')
            .then(response => response.json())
            .then(data => {
                const productList = document.getElementById('product-list');
                data.products.forEach(product => {
                    const productCard = \`
                        <div class="col-md-4 mb-4">
                            <div class="card product-card h-100">
                                <div class="card-body text-center">
                                    <i class="fas fa-cube fa-3x text-primary mb-3"></i>
                                    <h5 class="card-title">\${product.name}</h5>
                                    <p class="card-text text-muted">\${product.category}</p>
                                    <h6 class="text-success mb-3">$\${product.price}</h6>
                                    <button class="btn btn-primary" onclick="addToCart(\${product.id})">
                                        <i class="fas fa-cart-plus me-1"></i>Add to Cart
                                    </button>
                                </div>
                            </div>
                        </div>
                    \`;
                    productList.innerHTML += productCard;
                });
            })
            .catch(error => {
                console.error('Error loading products:', error);
                document.getElementById('product-list').innerHTML = 
                    '<div class="col-12"><div class="alert alert-warning">Mock products failed to load</div></div>';
            });

        function addToCart(productId) {
            alert(\`Product \${productId} added to cart! (Mock action)\`);
        }
    </script>
</body>
</html>`;

  res.writeHead(200, { "Content-Type": "text/html" });
  res.end(html);
}

function serveShoppingPage(pathname, res) {
  const html = `
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>ToyShop - ${pathname}</title>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css" rel="stylesheet">
    <link href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.0.0/css/all.min.css" rel="stylesheet">
</head>
<body>
    <nav class="navbar navbar-expand-lg navbar-dark bg-dark">
        <div class="container">
            <a class="navbar-brand" href="/">
                <i class="fas fa-store me-2"></i>ToyShop
            </a>
        </div>
    </nav>

    <div class="container mt-5">
        <div class="row">
            <div class="col-12">
                <div class="alert alert-info">
                    <h4><i class="fas fa-construction me-2"></i>Page: ${pathname}</h4>
                    <p>This page is in development. In the full application, this would show:</p>
                    <ul>
                        <li><strong>/Product:</strong> Product details and catalog</li>
                        <li><strong>/Cart:</strong> Shopping cart management</li>
                        <li><strong>/Order:</strong> Order history and management</li>
                    </ul>
                    <a href="/" class="btn btn-primary">
                        <i class="fas fa-home me-1"></i>Back to Home
                    </a>
                </div>
            </div>
        </div>
    </div>
</body>
</html>`;

  res.writeHead(200, { "Content-Type": "text/html" });
  res.end(html);
}

function serve404(res) {
  res.writeHead(404, { "Content-Type": "text/html" });
  res.end(`
    <html>
      <body style="font-family: Arial, sans-serif; text-align: center; padding: 50px;">
        <h1>404 - Page Not Found</h1>
        <p>The requested page was not found.</p>
        <a href="/" style="color: #007bff;">Back to Home</a>
      </body>
    </html>
  `);
}

server.listen(PORT, () => {
  console.log(`🛍️  ToyShop Development Server`);
  console.log(`📍 Server running at http://localhost:${PORT}`);
  console.log(`🔓 Authentication: Bypassed for development`);
  console.log(`📦 APIs: Mock responses enabled`);
  console.log(`👤 User: admin@toyshop.com (Mock)`);
});
