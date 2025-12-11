import {createBrowserRouter, RouterProvider} from "react-router";
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
//import Auth from "@components/routes/auth/Auth.tsx";

    export default function App() {
        const [username, setUsername] = useState<string | null>(null);
        const [isAdmin, setIsAdmin] = useState(false);
        const [userType, setUserType] = useState<'user' | 'admin'>('user');
        const [showProfile, setShowProfile] = useState(false);
        const [showUserManagement, setShowUserManagement] = useState(false);

        const handleLogin = (user: string, admin: boolean) => {
            setUsername(user);
            setIsAdmin(admin);
            setUserType(admin ? 'admin' : 'user');
        };

        const handleLogout = () => {
            setUsername(null);
            setIsAdmin(false);
        };

        // Show login screen if not logged in
        if (!username) {
            return <LoginPage onLogin={handleLogin} />;
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
                                        onClick={() => setUserType('user')}
                                        className={`px-4 py-2 rounded-md transition-colors ${
                                            userType === 'user'
                                                ? 'bg-[#ed1c24] text-white'
                                                : 'text-gray-700 hover:bg-gray-100'
                                        }`}
                                    >
                                        Spil
                                    </button>
                                    <button
                                        onClick={() => setUserType('admin')}
                                        className={`px-4 py-2 rounded-md transition-colors ${
                                            userType === 'admin' && !showUserManagement
                                                ? 'bg-[#ed1c24] text-white'
                                                : 'text-gray-700 hover:bg-gray-100'
                                        }`}
                                    >
                                        Administrer spillere
                                    </button>
                                    {userType === 'admin' && (
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
                    {userType === 'user' ? <UserView /> : <AdminPage />}
                </main>

                {/* Profile Modal */}
                {showProfile && (
                    <ProfileModal
                        isAdmin={userType === 'admin'}
                        onClose={() => setShowProfile(false)}
                        showUserManagement={showUserManagement}
                        onCloseUserManagement={() => setShowUserManagement(false)}
                    />
                )}

                {/* User Management Modal (Admin Only) */}
                {showUserManagement && userType === 'admin' && (
                    <ProfileModal
                        isAdmin={true}
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


