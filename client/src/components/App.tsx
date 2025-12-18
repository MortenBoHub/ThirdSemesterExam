import {createBrowserRouter, RouterProvider} from "react-router-dom";
import Home from "@components/Home.tsx";
import {DevTools} from "jotai-devtools";
//import 'jotai-devtools/styles.css'
import {Toaster} from "react-hot-toast";
import LoginPage from "./loginPage";
import UserView from "./userPage";
import AdminPage from "./adminPage";
import ProfileModal from "./ProfileModal";
import { User, Users, LogOut } from 'lucide-react';
import {useState} from "react";
import { useEffect } from "react";
import { authApi } from "../utilities/authApi";
import type { JwtClaims } from "@core/generated-client.ts";
import { Navigate } from "react-router-dom";
import { useNavigate } from "react-router-dom";
import { useLocation } from "react-router-dom";
import { Routes, Route} from "react-router-dom";
//import Auth from "@components/routes/auth/Auth.tsx";

function RequireAuth(props: { isAuthed: boolean; children: React.ReactElement }) {
    return props.isAuthed ? props.children : <Navigate to="/login" replace />;
}

function RequireAdmin(props: { isAdmin: boolean; children: React.ReactElement }) {
    return props.isAdmin ? props.children : <Navigate to="/user" replace />;
}
export default function App() {
        const [showProfile, setShowProfile] = useState(false);
        const [showUserManagement, setShowUserManagement] = useState(false);
        const [claims, setClaims] = useState<JwtClaims | null>(null);
        const [authChecked, setAuthChecked] = useState(false);
        const isAdminRole = claims?.role === "Admin";
        const navigate = useNavigate();
        const location = useLocation();
        const path = location.pathname;
        
        useEffect(() => {
            const token = localStorage.getItem("jwt");

            if (!token) {
                setAuthChecked(true);
                return;
            }

            authApi
                .whoAmI()
                .then((c) => setClaims(c))
                .catch(() => {
                    localStorage.removeItem("jwt");
                    setClaims(null);
                })
                .finally(() => setAuthChecked(true));
        }, []);
        const handleLogout = () => {
            localStorage.removeItem("jwt");
            setClaims(null);
        };

        // Show login screen if not logged in
        if (!authChecked) {
            return <div className="p-6">Loading...</div>;
        }

        if (!claims) {
            return <LoginPage onLogin={() => authApi.whoAmI().then(setClaims)} />;
        }

        return (
        <>
            <div className="min-h-screen bg-[#f5f1e8]">
                {/* Navigation */}
                <nav className="bg-white border-b-2 border-[#ed1c24] shadow-sm">
                    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
                        <div className="flex justify-between items-center h-16">
                            <div className="flex items-center space-x-8">
                                <h1 className="text-[#ed1c24]">🎯 Døde Duer</h1>
                                <div className="flex space-x-4">
                                    <button
                                        onClick={() => navigate("/user")}
                                        className={`px-4 py-2 rounded-md transition-colors ${
                                            path === "/user"
                                                ? 'bg-[#ed1c24] text-white'
                                                : 'text-gray-700 hover:bg-gray-100'
                                        }`}
                                    >
                                        Spil
                                    </button>
                                    {isAdminRole && (
                                        <button
                                            onClick={() => {
                                                if (isAdminRole) navigate("/admin");
                                            }}
                                            className={`px-4 py-2 rounded-md transition-colors ${
                                                path === "/admin" && !showUserManagement
                                                    ? 'bg-[#ed1c24] text-white'
                                                    : 'text-gray-700 hover:bg-gray-100'
                                            }`}
                                        >
                                            Administrer spillere
                                        </button>
                                    )}
                                    {isAdminRole && path === "/admin" && (
                                        <button
                                            onClick={() => setShowUserManagement(true)}
                                            className={`px-4 py-2 rounded-md transition-colors flex items-center space-x-2 ${
                                                showUserManagement
                                                    ? 'bg-[#ed1c24] text-white'
                                                    : 'text-gray-700 hover:bg-gray-100'
                                            }`}
                                        >
                                            <Users size={20} />
                                            <span>Brugere</span>
                                        </button>
                                    )}
                                </div>
                            </div>
                            <div className="flex items-center space-x-2">
                                <button
                                    onClick={() => setShowProfile(true)}
                                    className="flex items-center space-x-2 px-4 py-2 rounded-md text-gray-700 hover:bg-gray-100 transition-colors"
                                >
                                    <User size={20} />
                                    <span>Profil</span>
                                </button>
                                <button
                                    onClick={handleLogout}
                                    className="flex items-center space-x-2 px-4 py-2 rounded-md text-gray-700 hover:bg-gray-100 transition-colors"
                                >
                                    <LogOut size={20} />
                                    <span>Log ud</span>
                                </button>
                            </div>
                        </div>
                    </div>
                </nav>

                {/* Main Content */}
                <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
                <Routes>
                    <Route
                        path="/"
                        element={claims ? <Navigate to="/user" replace /> : <Navigate to="/login" replace />}
                    />
                    <Route
                        path="/login"
                        element={<LoginPage onLogin={() => authApi.whoAmI().then(setClaims)} />}
                    />
                    <Route
                        path="/user"
                        element={
                            <RequireAuth isAuthed={!!claims}>
                                <UserView claims={claims!} />
                            </RequireAuth>
                        }
                    />
                    <Route
                        path="/admin"
                        element={
                            <RequireAuth isAuthed={!!claims}>
                                <RequireAdmin isAdmin={isAdminRole}>
                                    <AdminPage />
                                </RequireAdmin>
                            </RequireAuth>
                        }
                    />
                    <Route path="*" element={<Navigate to="/" replace />} />
                </Routes>
            </main>

                {/* Profile Modal */}
                {showProfile && (
                    <ProfileModal
                        isAdmin={isAdminRole}
                        onClose={() => setShowProfile(false)}
                        showUserManagement={showUserManagement}
                        onCloseUserManagement={() => setShowUserManagement(false)}
                    />
                )}

                {/* User Management Modal (Admin Only) */}
                {showUserManagement && isAdminRole && path === "/admin" && (
                    <ProfileModal
                        isAdmin={isAdminRole}
                        onClose={() => setShowUserManagement(false)}
                        showUserManagement={true}
                        onCloseUserManagement={() => setShowUserManagement(false)}
                    />
                )}
            </div>
            <DevTools/>
            <Toaster
                position="top-center"
                reverseOrder={false}
            />
        </>
    )
}


