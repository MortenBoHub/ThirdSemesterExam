import { useEffect, useState } from 'react';
import type React from 'react';
import {
    X,
    DollarSign,
    Mail,
    Phone,
    Lock,
    Plus,
    Edit,
    Trash2,
    CheckCircle,
    AlertCircle,
    ArrowLeft,
} from 'lucide-react';
import { Button } from './UI/Button';
import { Input } from './UI/Input';
import { Card, CardContent, CardHeader, CardTitle } from './UI/Card';
import { Badge } from './UI/Badge';
import { Tabs, TabsContent, TabsList, TabsTrigger } from './UI/Tabs';
import {
    Dialog,
    DialogContent,
    DialogHeader,
    DialogTitle,
    DialogDescription,
    DialogFooter,
} from './UI/Dialog';
import { authApi } from '@utilities/authApi.ts';
import { playersApi } from '@utilities/playersApi.ts';
import toast from 'react-hot-toast';
import { fundRequestsApi } from '@utilities/fundRequestsApi.ts';

interface ProfileModalProps {
    isAdmin: boolean;
    onClose: () => void;
    showUserManagement?: boolean;
    onCloseUserManagement?: () => void;
}

interface FundRequest {
    id: string; // backend id
    amount: number;
    transactionNumber: string;
    status: 'pending' | 'approved' | 'denied';
    date: string; // createdAt
}

interface User {
    id: number; // local list id
    backendId?: string; // real player id from API
    name: string;
    email: string;
    phone: string;
    funds: number;
    isDeleted: boolean;
    fundsRequests: FundRequest[];
}

export default function ProfileModal({
                                 isAdmin,
                                 onClose,
                                 showUserManagement = false,
                                 onCloseUserManagement,
                             }: ProfileModalProps) {
    const [activeTab, setActiveTab] = useState(showUserManagement ? 'users' : 'info');
    const [showFundRequestDialog, setShowFundRequestDialog] = useState(false);
    const [fundAmount, setFundAmount] = useState('');
    const [transactionNumber, setTransactionNumber] = useState('');
    const [fundRequestSubmitted, setFundRequestSubmitted] = useState(false);
    const [editingUser, setEditingUser] = useState<User | null>(null);
    const [creatingUser, setCreatingUser] = useState(false);
    const [editingUserFunds, setEditingUserFunds] = useState<User | null>(null);

    const [currentUser, setCurrentUser] = useState<{ id?: string; name: string; email: string; phone?: string; funds?: number }>({
        name: '-',
        email: '-',
        phone: '',
        funds: 0,
    });

    const [users, setUsers] = useState<User[]>([]);
    const [loading, setLoading] = useState<boolean>(false);

    useEffect(() => {
        const load = async () => {
            setLoading(true);
            try {
                const me = await authApi.whoAmI();
                if (me && me.id) {
                    try {
                        const player = await playersApi.getPlayer(me.id);
                        setCurrentUser({
                            id: me.id,
                            name: player.name ?? '-',
                            email: player.email ?? me.email ?? '-',
                            phone: player.phonenumber ?? '',
                            funds: player.funds ?? 0,
                        });
                    } catch (e:any) {
                        setCurrentUser({ id: me.id, name: me.email ?? '-', email: me.email ?? '-' });
                    }
                }

                if (isAdmin) {
                    const list = await playersApi.getPlayers();
                    const mappedUsers: User[] = (list as any[]).map((p, idx) => ({
                        id: idx + 1,
                        backendId: p.id,
                        name: p.name ?? '-',
                        email: p.email ?? '-',
                        phone: p.phonenumber ?? '',
                        funds: p.funds ?? 0,
                        isDeleted: p.isdeleted ?? false,
                        fundsRequests: [],
                    }));

                    // Load pending fund requests and attach to users
                    try {
                        const pending = await fundRequestsApi.list('pending');
                        const byPlayer: Record<string, FundRequest[]> = {};
                        (pending as any[]).forEach(fr => {
                            const r: FundRequest = {
                                id: fr.id,
                                amount: fr.amount ?? 0,
                                transactionNumber: fr.transactionnumber ?? fr.transactionNumber ?? '',
                                status: (fr.status ?? 'pending') as 'pending'|'approved'|'denied',
                                date: fr.createdat ?? fr.createdAt ?? new Date().toISOString(),
                            };
                            const pid: string = fr.playerid ?? fr.playerId;
                            if (!byPlayer[pid]) byPlayer[pid] = [];
                            byPlayer[pid].push(r);
                        });
                        // Sort each player's requests by oldest first (createdAt ascending)
                        Object.keys(byPlayer).forEach(pid => {
                            byPlayer[pid].sort((a, b) => new Date(a.date).getTime() - new Date(b.date).getTime());
                        });
                        mappedUsers.forEach(u => {
                            if (u.backendId && byPlayer[u.backendId]) {
                                u.fundsRequests = byPlayer[u.backendId];
                            }
                        });
                        // Sort users by oldest pending request date (ascending). Users without pending requests come after.
                        mappedUsers.sort((a, b) => {
                            const aPending = a.fundsRequests.filter(r => r.status === 'pending');
                            const bPending = b.fundsRequests.filter(r => r.status === 'pending');
                            const aDate = aPending.length > 0 ? new Date(aPending[0].date).getTime() : Number.POSITIVE_INFINITY;
                            const bDate = bPending.length > 0 ? new Date(bPending[0].date).getTime() : Number.POSITIVE_INFINITY;
                            return aDate - bDate; // oldest first
                        });
                    } catch { /* ignore pending load errors here */ }

                    setUsers(mappedUsers);
                }
            } catch (e:any) {
                toast.error(e?.message ?? 'Kunne ikke hente profil data');
            } finally {
                setLoading(false);
            }
        };
        load();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [isAdmin]);

    const handleRequestFunds = () => {
        setShowFundRequestDialog(true);
    };

    const submitFundRequest = async () => {
        const amountNum = parseFloat(fundAmount);
        if (!(amountNum > 0)) {
            toast.error('Beløb skal være større end 0');
            return;
        }
        if (!transactionNumber.trim()) {
            toast.error('Transaktionsnummer er påkrævet');
            return;
        }
        try {
            setFundRequestSubmitted(true);
            await fundRequestsApi.create({ amount: amountNum, transactionNumber: transactionNumber.trim() });
            toast.success('Anmodning sendt');
            setShowFundRequestDialog(false);
            setFundAmount('');
            setTransactionNumber('');
        } catch (e:any) {
            toast.error(e?.message ?? 'Kunne ikke sende anmodning');
        } finally {
            setFundRequestSubmitted(false);
        }
    };

    const handleSoftDelete = async (userId: number) => {
        try {
            const user = users.find(u => u.id === userId);
            if (!user || !user.backendId) throw new Error('Ukendt bruger');
            await playersApi.softDelete(user.backendId);
            setUsers(users.map((u) => (u.id === userId ? { ...u, isDeleted: true } : u)));
            toast.success('Bruger deaktiveret');
        } catch (e:any) {
            toast.error(e?.message ?? 'Kunne ikke deaktivere bruger');
        }
    };

    const handleRestore = async (userId: number) => {
        try {
            const user = users.find(u => u.id === userId);
            if (!user || !user.backendId) throw new Error('Ukendt bruger');
            await playersApi.restore(user.backendId);
            setUsers(users.map((u) => (u.id === userId ? { ...u, isDeleted: false } : u)));
            toast.success('Bruger gendannet');
        } catch (e:any) {
            toast.error(e?.message ?? 'Kunne ikke gendanne bruger');
        }
    };

    const handleUpdateUser = async (updatedUser: User) => {
        try {
            if (!updatedUser.backendId) throw new Error('Ukendt bruger');
            await playersApi.updatePlayer(updatedUser.backendId, {
                name: updatedUser.name,
                email: updatedUser.email,
                phonenumber: updatedUser.phone,
            });
            setUsers(users.map((u) => (u.id === updatedUser.id ? updatedUser : u)));
            setEditingUser(null);
            toast.success('Bruger opdateret');
        } catch (e:any) {
            toast.error(e?.message ?? 'Kunne ikke opdatere bruger');
        }
    };

    const handleCreateUser = async (newUser: Omit<User, 'id'>) => {
        try {
            // Map to API DTO
            const created = await playersApi.createPlayer({
                name: newUser.name,
                email: newUser.email,
                phoneNumber: newUser.phone,
                password: 'changeme123', // Temporary default; could prompt in UI
            });
            const id = (users.map((u) => u.id).length > 0 ? Math.max(...users.map((u) => u.id)) : 0) + 1;
            const userToAdd: User = {
                id,
                backendId: created.id,
                name: created.name ?? newUser.name,
                email: created.email ?? newUser.email,
                phone: created.phonenumber ?? newUser.phone,
                funds: created.funds ?? 0,
                isDeleted: created.isdeleted ?? false,
                fundsRequests: [],
            };
            setUsers([...users, userToAdd]);
            setCreatingUser(false);
            toast.success('Bruger oprettet');
        } catch (e:any) {
            toast.error(e?.message ?? 'Kunne ikke oprette bruger');
        }
    };

    const handleApproveFundRequest = async (userId: number, requestId: string) => {
        try {
            const user = users.find(u => u.id === userId);
            const req = user?.fundsRequests.find(r => r.id === requestId);
            if (!req) throw new Error('Ukendt anmodning');
            await fundRequestsApi.approve(requestId);
            setUsers(users.map(u => {
                if (u.id !== userId) return u;
                return {
                    ...u,
                    funds: u.funds + req.amount,
                    fundsRequests: u.fundsRequests.map(r => r.id === requestId ? { ...r, status: 'approved' } : r)
                };
            }));
            toast.success('Anmodning godkendt');
        } catch (e:any) {
            toast.error(e?.message ?? 'Kunne ikke godkende anmodning');
        }
    };

    const handleDenyFundRequest = async (userId: number, requestId: string) => {
        try {
            await fundRequestsApi.deny(requestId);
            setUsers(users.map(u => {
                if (u.id !== userId) return u;
                return {
                    ...u,
                    fundsRequests: u.fundsRequests.map(r => r.id === requestId ? { ...r, status: 'denied' } : r)
                };
            }));
            toast.success('Anmodning afvist');
        } catch (e:any) {
            toast.error(e?.message ?? 'Kunne ikke afvise anmodning');
        }
    };

    const pendingRequestsCount = users.reduce(
        (sum, u) => sum + u.fundsRequests.filter((r) => r.status === 'pending').length,
        0,
    );

    const handleClose = () => {
        if (showUserManagement && onCloseUserManagement) {
            onCloseUserManagement();
        } else {
            onClose();
        }
    };

    return (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4 overflow-y-auto">
            <div className="bg-white rounded-lg shadow-xl max-w-4xl w-full my-8 max-h-[90vh] flex flex-col">
                {/* Header */}
                <div className="flex items-center justify-between p-6 border-b">
                    <h2>{showUserManagement ? 'Brugerstyring' : 'Profil'}</h2>
                    <button
                        onClick={handleClose}
                        className="text-gray-500 hover:text-gray-700 transition-colors"
                    >
                        <X size={24} />
                    </button>
                </div>

                {/* Content */}
                <div className="p-6 overflow-y-auto">
                {isAdmin && showUserManagement ? (
                        <div className="space-y-4">
                            {/* Exit Button */}
                            <Button
                                onClick={handleClose}
                                className="border border-[#ed1c24] text-[#ed1c24] hover:bg-[#ed1c24] hover:text-white px-4 py-2 rounded"
                            >
                                <ArrowLeft size={20} className="mr-2 inline-block" />
                                Tilbage til Administrer spillere
                            </Button>

                            <Tabs value={activeTab} onValueChange={setActiveTab} className="w-full mt-4">
                                <TabsList className="w-full grid grid-cols-2">
                                    <TabsTrigger value="users">Alle Brugere</TabsTrigger>
                                    <TabsTrigger value="requests">
                                        Penge Anmodninger
                                        {pendingRequestsCount > 0 && (
                                            <Badge className="ml-2 bg-[#ed1c24] text-white">
                                                {pendingRequestsCount}
                                            </Badge>
                                        )}
                                    </TabsTrigger>
                                </TabsList>

                                {/* Users Tab */}
                                <TabsContent value="users" className="space-y-4 mt-4">
                                    <div className="flex items-center justify-between mb-4">
                                        <h3></h3>
                                    </div>

                                    <div className="space-y-3">
                                        {users
                                            .filter((u) => !u.isDeleted)
                                            .map((user) => {
                                                const pendingForUser = user.fundsRequests.filter(
                                                    (r) => r.status === 'pending',
                                                ).length;

                                                return (
                                                    <div
                                                        key={user.id}
                                                        className="border rounded-lg p-4 hover:border-[#ed1c24] transition-colors"
                                                    >
                                                        <div className="flex items-start justify-between">
                                                            <div className="space-y-1 flex-1">
                                                                <div className="flex items-center space-x-3">
                                                                    <p>{user.name}</p>
                                                                    {pendingForUser > 0 && (
                                                                        <Badge className="bg-amber-500 text-white">
                                                                            {pendingForUser} anmodning
                                                                            {pendingForUser !== 1 ? 'er' : ''}
                                                                        </Badge>
                                                                    )}
                                                                </div>
                                                                <p className="text-sm text-gray-600">{user.email}</p>
                                                                <p className="text-sm text-gray-600">{user.phone}</p>
                                                                <div className="flex items-center space-x-3 mt-2">
                                                                    <div className="flex items-center space-x-2">
                                                                        <DollarSign size={16} className="text-green-600" />
                                                                        <span className="text-sm">{user.funds} kr</span>
                                                                    </div>
                                                                    <Button
                                                                        onClick={() => setEditingUserFunds(user)}
                                                                        className="h-7 px-2 text-sm border border-green-500 text-green-600 hover:bg-green-50 rounded"
                                                                    >
                                                                        <Edit size={14} />
                                                                    </Button>
                                                                </div>
                                                            </div>
                                                            <div className="flex space-x-2">
                                                                <Button
                                                                    onClick={() => setEditingUser(user)}
                                                                    className="px-3 py-1 border border-blue-500 text-blue-600 hover:bg-blue-50 rounded"
                                                                >
                                                                    <Edit size={16} />
                                                                </Button>
                                                                <Button
                                                                    onClick={() => handleSoftDelete(user.id)}
                                                                    className="px-3 py-1 border border-red-500 text-red-600 hover:bg-red-50 rounded"
                                                                >
                                                                    <Trash2 size={16} />
                                                                </Button>
                                                            </div>
                                                        </div>
                                                    </div>
                                                );
                                            })}
                                    </div>

                                    {users.filter((u) => u.isDeleted).length > 0 && (
                                        <div className="mt-8">
                                            <h3 className="mb-4 text-gray-600">Slettede Brugere</h3>
                                            <div className="space-y-3">
                                                {users
                                                    .filter((u) => u.isDeleted)
                                                    .map((user) => (
                                                        <div
                                                            key={user.id}
                                                            className="border border-gray-300 rounded-lg p-4 bg-gray-50 opacity-60"
                                                        >
                                                            <div className="flex items-start justify-between">
                                                                <div className="space-y-1">
                                                                    <p className="line-through">{user.name}</p>
                                                                    <p className="text-sm text-gray-600">{user.email}</p>
                                                                </div>
                                                                <Button
                                                                    onClick={() => handleRestore(user.id)}
                                                                    className="px-3 py-1 border border-green-500 text-green-600 hover:bg-green-50 rounded"
                                                                >
                                                                    Gendan
                                                                </Button>
                                                            </div>
                                                        </div>
                                                    ))}
                                            </div>
                                        </div>
                                    )}
                                </TabsContent>

                                {/* Fund Requests Tab */}
                                <TabsContent value="requests" className="space-y-4 mt-4">
                                    <h3></h3>

                                    {users
                                        .flatMap((u) =>
                                            u.fundsRequests
                                                .filter((r) => r.status === 'pending')
                                                .map((r) => ({ ...r, user: u })),
                                        )
                                        .map((_) => _).length === 0 ? (
                                        <div className="text-center py-8 text-gray-500">
                                            Ingen ventende anmodninger
                                        </div>
                                    ) : (
                                        <div className="space-y-3">
                                            {users
                                                .flatMap((u) =>
                                                    u.fundsRequests
                                                        .filter((r) => r.status === 'pending')
                                                        .map((r) => ({ ...r, user: u })),
                                                )
                                                .map((request) => (
                                                    <Card
                                                        key={`${request.user.id}-${request.id}`}
                                                        className="border-2 border-amber-200 bg-amber-50/30"
                                                    >
                                                        <CardContent className="pt-6">
                                                            <div className="flex items-start justify-between">
                                                                <div className="space-y-2 flex-1">
                                                                    <div className="flex items-center space-x-2">
                                                                        <p>{request.user.name}</p>
                                                                        <Badge className="bg-amber-500 text-white">Afventer</Badge>
                                                                    </div>
                                                                    <p className="text-sm text-gray-600">
                                                                        {request.user.email}
                                                                    </p>
                                                                    <div className="space-y-1 text-sm">
                                                                        <p>
                                                                            <strong>Beløb:</strong>{' '}
                                                                            <span className="text-[#ed1c24]">
                                        {request.amount} kr
                                      </span>
                                                                        </p>
                                                                        <p>
                                                                            <strong>Transaktionsnr:</strong>{' '}
                                                                            {request.transactionNumber}
                                                                        </p>
                                                                        <p className="text-gray-500">
                                                                            Dato: {request.date}
                                                                        </p>
                                                                    </div>
                                                                </div>
                                                                <div className="flex flex-col space-y-2 ml-4">
                                                                    <Button
                                                                        onClick={() =>
                                                                            handleApproveFundRequest(
                                                                                request.user.id,
                                                                                request.id,
                                                                            )
                                                                        }
                                                                        className="px-3 py-1 bg-green-600 hover:bg-green-700 text-white rounded"
                                                                    >
                                                                        <CheckCircle size={16} className="mr-2 inline-block" />
                                                                        Godkend
                                                                    </Button>
                                                                    <Button
                                                                        onClick={() =>
                                                                            handleDenyFundRequest(request.user.id, request.id)
                                                                        }
                                                                        className="px-3 py-1 border border-red-500 text-red-600 hover:bg-red-50 rounded"
                                                                    >
                                                                        <X size={16} className="mr-2 inline-block" />
                                                                        Afvis
                                                                    </Button>
                                                                </div>
                                                            </div>
                                                        </CardContent>
                                                    </Card>
                                                ))}
                                        </div>
                                    )}

                                    {/* History of processed requests */}
                                    {users
                                        .flatMap((u) =>
                                            u.fundsRequests
                                                .filter((r) => r.status !== 'pending')
                                                .map((r) => ({ ...r, user: u })),
                                        )
                                        .length > 0 && (
                                        <div className="mt-8">
                                            <h3 className="mb-4 text-gray-600">Tidligere anmodninger</h3>
                                            <div className="space-y-2">
                                                {users
                                                    .flatMap((u) =>
                                                        u.fundsRequests
                                                            .filter((r) => r.status !== 'pending')
                                                            .map((r) => ({ ...r, user: u })),
                                                    )
                                                    .slice(0, 5)
                                                    .map((request) => (
                                                        <div
                                                            key={`${request.user.id}-${request.id}`}
                                                            className="border rounded-lg p-3 bg-gray-50"
                                                        >
                                                            <div className="flex items-center justify-between">
                                                                <div className="space-y-1">
                                                                    <div className="flex items-center space-x-2">
                                    <span className="text-sm">
                                      {request.user.name}
                                    </span>
                                                                        <span className="text-xs text-gray-500">•</span>
                                                                        <span className="text-sm text-gray-600">
                                      {request.amount} kr
                                    </span>
                                                                    </div>
                                                                    <p className="text-xs text-gray-500">
                                                                        {request.date}
                                                                    </p>
                                                                </div>
                                                                <Badge
                                                                    className={
                                                                        request.status === 'approved'
                                                                            ? 'bg-green-600 text-white'
                                                                            : 'bg-red-600 text-white'
                                                                    }
                                                                >
                                                                    {request.status === 'approved'
                                                                        ? 'Godkendt'
                                                                        : 'Afvist'}
                                                                </Badge>
                                                            </div>
                                                        </div>
                                                    ))}
                                            </div>
                                        </div>
                                    )}
                                </TabsContent>
                            </Tabs>
                        </div>
                    ) : (
                        <ProfileInfoTab
                            user={currentUser}
                            isAdmin={isAdmin}
                            onRequestFunds={handleRequestFunds}
                            onSave={async (vals) => {
                                try {
                                    if (!currentUser.id) throw new Error('Ingen bruger-id fundet');
                                    // Update email/phone if changed
                                    const upd: any = {};
                                    if (vals.email && vals.email !== currentUser.email) upd.email = vals.email;
                                    if (vals.phone && vals.phone !== currentUser.phone) upd.phonenumber = vals.phone;
                                    if (Object.keys(upd).length > 0) {
                                        await playersApi.updatePlayer(currentUser.id, upd);
                                    }

                                    // Change password if provided
                                    if (vals.currentPassword && vals.newPassword) {
                                        await playersApi.changePassword(currentUser.id, vals.currentPassword, vals.newPassword);
                                    }

                                    // Refresh me
                                    try {
                                        const player = await playersApi.getPlayer(currentUser.id);
                                        setCurrentUser((prev) => ({
                                            ...prev,
                                            email: player.email ?? prev.email,
                                            phone: player.phonenumber ?? prev.phone,
                                        }));
                                    } catch {}

                                    toast.success('Ændringer gemt');
                                } catch (e:any) {
                                    toast.error(e?.message ?? 'Kunne ikke gemme ændringer');
                                }
                            }}
                        />
                    )}
                </div>
            </div>

            {/* Edit User Dialog */}
            {editingUser && (
                <UserEditDialog
                    user={editingUser}
                    onClose={() => setEditingUser(null)}
                    onSave={handleUpdateUser}
                />
            )}

            {/* Fund Request Dialog */}
            {showFundRequestDialog && (
                <Dialog open={true} onOpenChange={() => setShowFundRequestDialog(false)}>
                    <DialogContent className="max-w-md bg-white">
                        <DialogHeader>
                            <DialogTitle>Anmod om midler</DialogTitle>
                            <DialogDescription>
                                Udfyld beløbet og transaktionsnummeret fra din overførsel på MobilePay.
                            </DialogDescription>
                        </DialogHeader>
                        <div className="py-4 space-y-5">
                            {/* Amount */}
                            <div className="flex flex-col space-y-2">
                                <label className="text-sm text-gray-700">Beløb (kr)</label>
                                <Input
                                    type="number"
                                    min="1"
                                    value={fundAmount}
                                    onChange={(e) => setFundAmount(e.target.value)}
                                    placeholder="F.eks. 200"
                                    className="h-12 text-lg"
                                />
                            </div>

                            {/* Transaction number */}
                            <div className="flex flex-col space-y-2">
                                <label className="text-sm text-gray-700">
                                    Transaktionsnummer
                                </label>
                                <Input
                                    type="text"
                                    maxLength={50}
                                    value={transactionNumber}
                                    onChange={(e) => setTransactionNumber(e.target.value)}
                                    placeholder="F.eks. TXN123456789"
                                    className="h-12 text-lg"
                                />
                                <p className="text-xs text-gray-500">
                                    {transactionNumber.length}
                                </p>
                            </div>
                        </div>
                        <DialogFooter>
                            
                            <Button
                                className="px-4 py-2 bg-[#ed1c24] hover:bg-[#d11920] text-white rounded"
                                onClick={submitFundRequest}
                                disabled={
                                    !fundAmount ||
                                    !transactionNumber ||
                                    transactionNumber.length > 20 ||
                                    fundRequestSubmitted
                                }
                            >
                                {fundRequestSubmitted ? (
                                    <>
                                        <CheckCircle size={16} className="mr-2 inline-block" />
                                        Sendt!
                                    </>
                                ) : (
                                    'Send anmodning'
                                )}
                            </Button>
                            <Button
                            onClick={() => setShowFundRequestDialog(false)}
                            className="px-4 py-2 border border-gray-300 rounded"
                        >
                            Annuller
                        </Button>
                        </DialogFooter>
                    </DialogContent>
                </Dialog>
            )}

            {/* Edit User Funds Dialog */}
            {editingUserFunds && (
                <FundsEditDialog
                    user={editingUserFunds}
                    onClose={() => setEditingUserFunds(null)}
                    onSave={(userId, newFunds) => {
                        setUsers(
                            users.map((u) => (u.id === userId ? { ...u, funds: newFunds } : u)),
                        );
                        setEditingUserFunds(null);
                    }}
                />
            )}
        </div>
    );
}

// Profile Info Component
function ProfileInfoTab({
                            user,
                            isAdmin,
                            onRequestFunds,
                            onSave,
                        }: {
    user: { name: string; email: string; phone?: string; funds?: number };
    isAdmin: boolean;
    onRequestFunds: () => void;
    onSave: (vals: { email?: string; phone?: string; currentPassword?: string; newPassword?: string }) => Promise<void> | void;
}) {
    const [email, setEmail] = useState(user.email ?? '');
    const [phone, setPhone] = useState(user.phone ?? '');
    const [currentPassword, setCurrentPassword] = useState('');
    const [newPassword, setNewPassword] = useState('');
    const [confirmPassword, setConfirmPassword] = useState('');
    const [saving, setSaving] = useState(false);

    useEffect(() => {
        setEmail(user.email ?? '');
        setPhone(user.phone ?? '');
    }, [user.email, user.phone]);

    const handleSave = async () => {
        if (newPassword || confirmPassword || currentPassword) {
            if (!currentPassword) {
                toast.error('Angiv nuværende kodeord');
                return;
            }
            if (newPassword !== confirmPassword) {
                toast.error('Kodeord stemmer ikke overens');
                return;
            }
            if (newPassword.length < 6) {
                toast.error('Nyt kodeord skal være mindst 6 tegn');
                return;
            }
        }

        setSaving(true);
        try {
            await onSave({
                email,
                phone,
                currentPassword: currentPassword || undefined,
                newPassword: newPassword || undefined,
            });
            setCurrentPassword('');
            setNewPassword('');
            setConfirmPassword('');
        } finally {
            setSaving(false);
        }
    };

    return (
        <div className="space-y-6">
            {/* Funds Card */}
            <Card className="border-2 border-[#ed1c24]/20">
                <CardHeader className="bg-gradient-to-r from-[#ed1c24]/5 to-transparent">
                    <CardTitle className="text-[#ed1c24] flex items-center space-x-2">
                        <DollarSign size={24} />
                        <span>Saldo</span>
                    </CardTitle>
                </CardHeader>
                <CardContent className="pt-6">
                    <div className="flex items-center justify-between mb-4">
                        <div>
                            <p className="text-sm text-gray-600">Nuværende saldo</p>
                            <p className="text-[#ed1c24]">{user.funds ?? 0} kr</p>
                        </div>
                    </div>
                    <Button
                        onClick={onRequestFunds}
                        className="w-full bg-[#ed1c24] hover:bg-[#d11920] text-white py-2 rounded"
                    >
                        Anmod om flere penge
                    </Button>
                </CardContent>
            </Card>

            {/* Account Info */}
            <Card>
                <CardHeader>
                    <CardTitle>Kontooplysninger</CardTitle>
                </CardHeader>
                <CardContent className="space-y-4">
                    <div className="space-y-2">
                        <label className="text-sm flex items-center space-x-2">
                            <Mail size={16} className="text-gray-500" />
                            <span>Email</span>
                        </label>
                        <Input value={email} onChange={(e) => setEmail(e.target.value)} />
                    </div>
                    <div className="space-y-2">
                        <label className="text-sm flex items-center space-x-2">
                            <Phone size={16} className="text-gray-500" />
                            <span>Telefon</span>
                        </label>
                        <Input value={phone} onChange={(e) => setPhone(e.target.value)} />
                    </div>
                    <div className="space-y-2">
                        <label className="text-sm flex items-center space-x-2">
                            <Lock size={16} className="text-gray-500" />
                            <span>Nuværende kodeord</span>
                        </label>
                        <Input type="password" placeholder="••••••••" value={currentPassword} onChange={(e)=>setCurrentPassword(e.target.value)} />
                    </div>
                    <div className="space-y-2">
                        <label className="text-sm flex items-center space-x-2">
                            <Lock size={16} className="text-gray-500" />
                            <span>Nyt kodeord</span>
                        </label>
                        <Input type="password" placeholder="••••••••" value={newPassword} onChange={(e)=>setNewPassword(e.target.value)} />
                    </div>
                    <div className="space-y-2">
                        <label className="text-sm flex items-center space-x-2">
                            <Lock size={16} className="text-gray-500" />
                            <span>Bekræft nyt kodeord</span>
                        </label>
                        <Input type="password" placeholder="••••••••" value={confirmPassword} onChange={(e)=>setConfirmPassword(e.target.value)} />
                    </div>
                    <Button onClick={handleSave} disabled={saving} className="w-full bg-[#ed1c24] hover:bg-[#d11920] text-white py-2 rounded">
                        Gem ændringer
                    </Button>
                </CardContent>
            </Card>
        </div>
    );
}

// User Edit Dialog
function UserEditDialog({
                            user,
                            onClose,
                            onSave,
                        }: {
    user: User;
    onClose: () => void;
    onSave: (user: User) => void;
}) {
    const [editedUser, setEditedUser] = useState(user);

    return (
        <Dialog open={true} onOpenChange={onClose}>
            <DialogContent className="max-w-md">
                <DialogHeader>
                    <DialogTitle>Rediger Bruger</DialogTitle>
                    <DialogDescription>
                        Rediger brugerens oplysninger nedenfor.
                    </DialogDescription>
                </DialogHeader>
                <div className="space-y-4 py-4">
                    <div className="space-y-2">
                        <label className="text-sm">Navn</label>
                        <Input
                            value={editedUser.name}
                            onChange={(e) =>
                                setEditedUser({ ...editedUser, name: e.target.value })
                            }
                        />
                    </div>
                    <div className="space-y-2">
                        <label className="text-sm">Email</label>
                        <Input
                            value={editedUser.email}
                            onChange={(e) =>
                                setEditedUser({ ...editedUser, email: e.target.value })
                            }
                        />
                    </div>
                    <div className="space-y-2">
                        <label className="text-sm">Telefon</label>
                        <Input
                            value={editedUser.phone}
                            onChange={(e) =>
                                setEditedUser({ ...editedUser, phone: e.target.value })
                            }
                        />
                    </div>
                </div>
                <DialogFooter>
                    <Button
                        onClick={onClose}
                        className="px-4 py-2 border border-gray-300 rounded"
                    >
                        Annuller
                    </Button>
                    <Button
                        className="px-4 py-2 bg-[#ed1c24] hover:bg-[#d11920] text-white rounded"
                        onClick={() => onSave(editedUser)}
                    >
                        Gem
                    </Button>
                </DialogFooter>
            </DialogContent>
        </Dialog>
    );
}

// User Create Dialog
function UserCreateDialog({
                              onClose,
                              onCreate,
                          }: {
    onClose: () => void;
    onCreate: (user: Omit<User, 'id'>) => void;
}) {
    const [newUser, setNewUser] = useState<Omit<User, 'id'>>({
        name: '',
        email: '',
        phone: '',
        funds: 0,
        isDeleted: false,
        fundsRequests: [],
    });

    return (
        <Dialog open={true} onOpenChange={onClose}>
            <DialogContent className="max-w-md">
                <DialogHeader>
                    <DialogTitle>Opret Ny Bruger</DialogTitle>
                    <DialogDescription>
                        Opret en ny bruger med nedenstående oplysninger.
                    </DialogDescription>
                </DialogHeader>
                <div className="space-y-4 py-4">
                    <div className="space-y-2">
                        <label className="text-sm">Navn</label>
                        <Input
                            value={newUser.name}
                            onChange={(e) =>
                                setNewUser({ ...newUser, name: e.target.value })
                            }
                            placeholder="Indtast navn"
                        />
                    </div>
                    <div className="space-y-2">
                        <label className="text-sm">Email</label>
                        <Input
                            type="email"
                            value={newUser.email}
                            onChange={(e) =>
                                setNewUser({ ...newUser, email: e.target.value })
                            }
                            placeholder="email@eksempel.dk"
                        />
                    </div>
                    <div className="space-y-2">
                        <label className="text-sm">Telefon</label>
                        <Input
                            value={newUser.phone}
                            onChange={(e) =>
                                setNewUser({ ...newUser, phone: e.target.value })
                            }
                            placeholder="+45 12 34 56 78"
                        />
                    </div>
                    <div className="space-y-2">
                        <label className="text-sm">Start saldo (kr)</label>
                        <Input
                            type="number"
                            value={newUser.funds}
                            onChange={(e) =>
                                setNewUser({
                                    ...newUser,
                                    funds: parseInt(e.target.value) || 0,
                                })
                            }
                        />
                    </div>
                </div>
                <DialogFooter>
                    <Button
                        onClick={onClose}
                        className="px-4 py-2 border border-gray-300 rounded"
                    >
                        Annuller
                    </Button>
                    <Button
                        className="px-4 py-2 bg-[#ed1c24] hover:bg-[#d11920] text-white rounded"
                        onClick={() => onCreate(newUser)}
                        disabled={!newUser.name || !newUser.email}
                    >
                        Opret
                    </Button>
                </DialogFooter>
            </DialogContent>
        </Dialog>
    );
}

// Funds Edit Dialog
function FundsEditDialog({
                             user,
                             onClose,
                             onSave,
                         }: {
    user: User;
    onClose: () => void;
    onSave: (userId: number, newFunds: number) => void;
}) {
    const [editMode, setEditMode] = useState<'set' | 'add' | 'subtract'>('set');
    const [amount, setAmount] = useState('');

    const calculateNewFunds = () => {
        const numAmount = parseInt(amount) || 0;
        if (editMode === 'set') {
            return numAmount;
        } else if (editMode === 'add') {
            return user.funds + numAmount;
        } else {
            return Math.max(0, user.funds - numAmount);
        }
    };

    const handleSave = () => {
        const newFunds = calculateNewFunds();
        onSave(user.id, newFunds);
    };

    return (
        <Dialog open={true} onOpenChange={onClose}>
            <DialogContent className="max-w-md">
                <DialogHeader>
                    <DialogTitle>Rediger Saldo - {user.name}</DialogTitle>
                    <DialogDescription>
                        Vælg hvordan du vil ændre brugerens saldo.
                    </DialogDescription>
                </DialogHeader>
                <div className="space-y-4 py-4">
                    <div className="bg-gray-50 border border-gray-200 rounded-lg p-4">
                        <p className="text-sm text-gray-600">Nuværende saldo</p>
                        <p className="text-[#ed1c24]">{user.funds} kr</p>
                    </div>

                    <div className="space-y-3">
                        <label className="text-sm">Vælg handling</label>
                        <div className="grid grid-cols-3 gap-2">
                            <Button
                                type="button"
                                onClick={() => setEditMode('set')}
                                className={`px-3 py-2 rounded ${
                                    editMode === 'set'
                                        ? 'bg-[#ed1c24] hover:bg-[#d11920] text-white'
                                        : 'border border-gray-300'
                                }`}
                            >
                                Sæt beløb til
                            </Button>
                            <Button
                                type="button"
                                onClick={() => setEditMode('add')}
                                className={`px-3 py-2 rounded ${
                                    editMode === 'add'
                                        ? 'bg-green-600 hover:bg-green-700 text-white'
                                        : 'border border-green-500 text-green-600'
                                }`}
                            >
                                + Tilføj
                            </Button>
                            <Button
                                type="button"
                                onClick={() => setEditMode('subtract')}
                                className={`px-3 py-2 rounded ${
                                    editMode === 'subtract'
                                        ? 'bg-orange-600 hover:bg-orange-700 text-white'
                                        : 'border border-orange-500 text-orange-600'
                                }`}
                            >
                                - Træk fra
                            </Button>
                        </div>
                    </div>

                    <div className="space-y-2">
                        <label className="text-sm">
                            {editMode === 'set' && 'Ny saldo (kr)'}
                            {editMode === 'add' && 'Beløb at tilføje (kr)'}
                            {editMode === 'subtract' && 'Beløb at trække fra (kr)'}
                        </label>
                        <Input
                            type="number"
                            min="0"
                            value={amount}
                            onChange={(e) => setAmount(e.target.value)}
                            placeholder="Indtast beløb"
                            className="text-lg"
                        />
                    </div>

                    {amount && (
                        <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
                            <p className="text-sm text-gray-600">Ny saldo bliver</p>
                            <p className="text-blue-900">{calculateNewFunds()} kr</p>
                        </div>
                    )}
                </div>
                <DialogFooter>
                    <Button
                        onClick={onClose}
                        className="px-4 py-2 border border-gray-300 rounded"
                    >
                        Annuller
                    </Button>
                    <Button
                        className="px-4 py-2 bg-[#ed1c24] hover:bg-[#d11920] text-white rounded"
                        onClick={handleSave}
                        disabled={!amount}
                    >
                        Gem ændring
                    </Button>
                </DialogFooter>
            </DialogContent>
        </Dialog>
    );
}
