import { BrowserRouter, Routes, Route, useNavigate} from 'react-router-dom'
import LandingPage from './pages/LandingPage'
import LoginPage from './pages/LoginPage'
import DashboardPage from './pages/DashboardPage'
import ProductsPage from './pages/ProductsPage'
import AppLayout from './components/AppLayout'
import { useEffect } from 'react'
import { setNavigator } from './services/navigation'

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
        </Route>
      </Routes>
    </BrowserRouter>
  )
}

export default App
