import { BrowserRouter, Routes, Route, useNavigate} from 'react-router-dom'
import LandingPage from './pages/LandingPage'
import LoginPage from './pages/LoginPage'
import DashboardPage from './pages/DashboardPage'
import ProductsPage from './pages/ProductsPage'
import AppLayout from './components/AppLayout'
import { useEffect } from 'react'
import { setNavigator } from './services/navigation'
import StoragesPage from './pages/StoragesPage'
import StockPage from './pages/StockPage'
import SuppliersPage from './pages/SuppliersPage'
import CustomersPage from './pages/CustomersPage'
import PurchaseOrdersPage from './pages/PurchaseOrdersPage'
import PurchaseOrderPage from './pages/PurchaseOrderPage'
import NotFoundPage from './pages/NotFoundPage'

function NavigatorSetup() {
  const navigate = useNavigate()

  useEffect(() => {
    setNavigator(navigate)
  }, [navigate])

  return null   
}

function App() {
  return (
    <BrowserRouter>
    <NavigatorSetup />
      <Routes>
        <Route path="/" element={<LandingPage />} />
        <Route path="/login" element={<LoginPage />} />
        <Route path="/app" element={<AppLayout />}>
          <Route index element={<DashboardPage />} />
          <Route path="products" element={<ProductsPage />} />
          <Route path="storages" element={<StoragesPage />} />
          <Route path="suppliers" element={<SuppliersPage />} />
          <Route path="customers" element={<CustomersPage />} />
          <Route path="stock" element={<StockPage />} />
          <Route path="purchases" element={<PurchaseOrdersPage />} />
          <Route path="purchases/:id" element={<PurchaseOrderPage />} />
        </Route>
        <Route path="*" element={<NotFoundPage />} />
      </Routes>
    </BrowserRouter>
  )
}

export default App
