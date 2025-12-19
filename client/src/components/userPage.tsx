import { useState, useEffect } from 'react';
import NumberBoard from './UI/NumberBoard';
import { Button } from './UI/Button';
import { Card, CardContent, CardHeader, CardTitle } from './UI/Card';
import { Checkbox } from './UI/Checkbox';
import { AlertCircle, CheckCircle2, Plus, Trash2 } from 'lucide-react';
import { Badge } from './UI/Badge';
import {
    Dialog,
    DialogContent,
    DialogHeader,
    DialogTitle,
    DialogDescription,
    DialogFooter,
} from './UI/Dialog';
import { Input } from './UI/Input';
import type {JwtClaims} from '@core/generated-client.ts';
import { playersApi } from '@utilities/playersApi.ts';
import { boardsApi } from '@utilities/boardsApi.ts';
import toast from 'react-hot-toast';

interface Board {
    id: number;
    selectedNumbers: number[];
    repeatWeeks: number;
}

interface UserViewProps {
    claims: JwtClaims;
}

const PRICES = {
    5: 20,
    6: 40,
    7: 80,
    8: 160,
};

export default function UserView({ claims }: UserViewProps) {
    const [boards, setBoards] = useState<Board[]>([
        { id: 1, selectedNumbers: [], repeatWeeks: 1 },
    ]);
    const [submitted, setSubmitted] = useState(false);
    const [showRepeatDialog, setShowRepeatDialog] = useState<number | null>(null);
    const [repeatWeeks, setRepeatWeeks] = useState('1');
    const [activeBoardLabel, setActiveBoardLabel] = useState<string>("Indlæser...");
    const [loading, setLoading] = useState(false);
    const [history, setHistory] = useState<any[]>([]);

    useEffect(() => {
        boardsApi.getActive().then(active => {
            if (active && active.week) {
                setActiveBoardLabel(`Nuværende spil: Uge ${active.week} ${active.year}`);
            } else {
                setActiveBoardLabel("Intet aktivt spil");
            }
        }).catch(() => {
            setActiveBoardLabel("Intet aktivt spil");
        });

        boardsApi.getHistory(10).then(h => {
            setHistory(h);
        }).catch(() => {});
    }, []);

    const handleNumberSelect = (boardId: number, num: number) => {
        setBoards(
            boards.map((board) => {
                if (board.id !== boardId) return board;

                const selected = board.selectedNumbers;
                if (selected.includes(num)) {
                    return {
                        ...board,
                        selectedNumbers: selected.filter((n) => n !== num),
                    };
                } else if (selected.length < 8) {
                    return {
                        ...board,
                        selectedNumbers: [...selected, num],
                    };
                }
                return board;
            }),
        );
    };

    const addBoard = () => {
        const newId = Math.max(...boards.map((b) => b.id), 0) + 1;
        setBoards([
            ...boards,
            { id: newId, selectedNumbers: [], repeatWeeks: 1 },
        ]);
    };

    const removeBoard = (boardId: number) => {
        if (boards.length > 1) {
            setBoards(boards.filter((b) => b.id !== boardId));
        }
    };

    const toggleRepeat = (boardId: number, isRepeating: boolean) => {
        if (isRepeating) {
            setShowRepeatDialog(boardId);
        } else {
            setBoards(
                boards.map((b) =>
                    b.id === boardId ? { ...b, repeatWeeks: 1 } : b,
                ),
            );
        }
    };

    const confirmRepeat = () => {
        if (showRepeatDialog !== null) {
            const weeks = parseInt(repeatWeeks) || 1;
            setBoards(
                boards.map((b) =>
                    b.id === showRepeatDialog
                        ? { ...b, repeatWeeks: weeks }
                        : b,
                ),
            );
        }
        setShowRepeatDialog(null);
        setRepeatWeeks('1');
    };

    const getPrice = (count: number): number => {
        if (count >= 5 && count <= 8) {
            return PRICES[count as keyof typeof PRICES];
        }
        return 0;
    };

    const handleSubmit = async () => {
        const allValid = boards.every(
            (b) =>
                b.selectedNumbers.length >= 5 &&
                b.selectedNumbers.length <= 8,
        );
        if (!allValid) return;

        setLoading(true);
        try {
            for (const board of boards) {
                await playersApi.createBoards(claims.id, {
                    selectedNumbers: board.selectedNumbers,
                    repeatWeeks: board.repeatWeeks
                });
            }
            setSubmitted(true);
            toast.success("Brætter tilmeldt!");
            setBoards([{ id: 1, selectedNumbers: [], repeatWeeks: 1 }]);
            setTimeout(() => setSubmitted(false), 3000);
        } catch (e: any) {
            toast.error(e?.message ?? "Kunne ikke tilmelde brætter. Tjek din saldo.");
        } finally {
            setLoading(false);
        }
    };

    const totalPrice = boards.reduce((sum, board) => {
        const price = getPrice(board.selectedNumbers.length);
        return sum + price * board.repeatWeeks;
    }, 0);

    const allBoardsValid = boards.every(
        (b) =>
            b.selectedNumbers.length >= 5 &&
            b.selectedNumbers.length <= 8,
    );

    return (
        <div className="max-w-4xl mx-auto space-y-6">
            {/* Game Header */}
            <Card className="border-2 border-[#ed1c24]/20">
                <CardHeader className="bg-gradient-to-r from-[#ed1c24]/5 to-transparent">
                    <CardTitle className="text-[#ed1c24]">
                        {activeBoardLabel}
                    </CardTitle>
                </CardHeader>
                <CardContent className="pt-6">
                    {/* Instructions */}
                    <div className="bg-blue-50 border border-blue-200 rounded-lg p-4 mb-4">
                        <p className="text-sm text-blue-900">
                            Vælg mellem <strong>5-8 numre</strong> på hvert
                            bræt. Du kan oprette flere brætter med forskellige
                            tal.
                        </p>
                        <div className="mt-2 text-sm text-blue-900">
                            <strong>Priser:</strong> 5 tal = 20kr • 6 tal =
                            40kr • 7 tal = 80kr • 8 tal = 160kr
                        </div>
                    </div>

                    {/* Boards */}
                    <div className="space-y-6">
                        {boards.map((board, index) => {
                            const isValid =
                                board.selectedNumbers.length >= 5 &&
                                board.selectedNumbers.length <= 8;
                            const price = getPrice(
                                board.selectedNumbers.length,
                            );

                            return (
                                <Card
                                    key={board.id}
                                    className="border-2 border-gray-200"
                                >
                                    <CardHeader className="pb-4">
                                        <div className="flex items-center justify-between">
                                            <h3>Bræt {index + 1}</h3>
                                            {boards.length > 1 && (
                                                <Button
                                                    onClick={() =>
                                                        removeBoard(board.id)
                                                    }
                                                    className="border-red-500 text-red-600 hover:bg-red-50 px-3 py-1 rounded"
                                                >
                                                    <Trash2
                                                        size={16}
                                                        className="mr-2"
                                                    />
                                                    Fjern bræt
                                                </Button>
                                            )}
                                        </div>
                                    </CardHeader>
                                    <CardContent className="space-y-4">
                                        {/* Selection Status */}
                                        <div className="flex items-center justify-between bg-gray-50 rounded-lg p-4">
                                            <div className="flex items-center space-x-2">
                                                {isValid ? (
                                                    <CheckCircle2
                                                        className="text-green-600"
                                                        size={20}
                                                    />
                                                ) : (
                                                    <AlertCircle
                                                        className="text-amber-600"
                                                        size={20}
                                                    />
                                                )}
                                                <span className="text-sm">
                                                    Valgte:{' '}
                                                    <strong>
                                                        {
                                                            board
                                                                .selectedNumbers
                                                                .length
                                                        }
                                                    </strong>{' '}
                                                    / 5-8 tal
                                                </span>
                                            </div>
                                            <div className="text-sm">
                                                Pris:{' '}
                                                <strong className="text-[#ed1c24]">
                                                    {price > 0
                                                        ? `${price} kr`
                                                        : '-'}
                                                </strong>
                                                {board.repeatWeeks > 1 && (
                                                    <span className="ml-2 text-gray-600">
                                                        ×{' '}
                                                        {
                                                            board.repeatWeeks
                                                        }{' '}
                                                        uger ={' '}
                                                        {price *
                                                            board.repeatWeeks}{' '}
                                                        kr
                                                    </span>
                                                )}
                                            </div>
                                        </div>

                                        {/* Number Board */}
                                        <NumberBoard
                                            selectedNumbers={
                                                board.selectedNumbers
                                            }
                                            onNumberSelect={(num) =>
                                                handleNumberSelect(
                                                    board.id,
                                                    num,
                                                )
                                            }
                                        />

                                        {/* Repeat Option */}
                                        <div className="flex items-center space-x-2 bg-gray-50 rounded-lg p-4 bg-white">
                                            <Checkbox
                                                id={`repeat-${board.id}`}
                                                checked={
                                                    board.repeatWeeks > 1
                                                }
                                                onCheckedChange={(checked) =>
                                                    toggleRepeat(
                                                        board.id,
                                                        checked as boolean,
                                                    )
                                                }
                                            />
                                            <label
                                                htmlFor={`repeat-${board.id}`}
                                                className="text-sm cursor-pointer"
                                            >
                                                Gentag hver uge
                                                {board.repeatWeeks > 1 && (
                                                    <span className="ml-2 text-[#ed1c24]">
                                                        ({board.repeatWeeks}{' '}
                                                        uger i træk)
                                                    </span>
                                                )}
                                            </label>
                                        </div>

                                        {!isValid &&
                                            board.selectedNumbers.length >
                                            0 && (
                                                <p className="text-sm text-amber-600 text-center">
                                                    {board.selectedNumbers
                                                        .length < 5
                                                        ? `Vælg mindst ${
                                                            5 -
                                                            board
                                                                .selectedNumbers
                                                                .length
                                                        } mere`
                                                        : 'Du har valgt for mange numre'}
                                                </p>
                                            )}
                                    </CardContent>
                                </Card>
                            );
                        })}
                    </div>

                    {/* Add Board Button */}
                    <Button
                        onClick={addBoard}
                        className="w-full mt-4 border-[#ed1c24] text-[#ed1c24] hover:bg-[#ed1c24] hover:text-white border-2 py-2 rounded flex items-center justify-center gap-2"
                    >
                        <Plus size={20} />
                        <span>Tilføj nyt bræt</span>
                    </Button>

                    {/* Total and Submit */}
                    <div className="mt-6 space-y-4">
                        <div className="bg-gradient-to-r from-[#ed1c24]/10 to-transparent rounded-lg p-4">
                            <div className="flex items-center justify-between">
                                <span>Total pris for alle brætter:</span>
                                <span className="text-[#ed1c24]">
                                    {totalPrice} kr
                                </span>
                            </div>
                        </div>

                        <Button
                            onClick={handleSubmit}
                            disabled={!allBoardsValid}
                            className="w-full bg-[#ed1c24] hover:bg-[#d11920] disabled:bg-gray-300 disabled:cursor-not-allowed py-3 rounded"
                        >
                            {submitted
                                ? 'Tilmeldt! ✓'
                                : 'Tilmeld alle brætter'}
                        </Button>
                    </div>
                </CardContent>
            </Card>

            {/* Previous Results */}
            <Card>
                <CardHeader>
                    <CardTitle>Tidligere resultater</CardTitle>
                </CardHeader>
                <CardContent>
                    <div className="max-h-[400px] overflow-y-auto pr-2 space-y-3">
                        {history.length === 0 ? (
                            <p className="text-center text-gray-500 py-4">Ingen historik fundet</p>
                        ) : (
                            history.map((result, idx) => (
                                <div
                                    key={idx}
                                    className={`flex items-center justify-between p-3 rounded-lg border ${
                                        result.winners > 0
                                            ? 'bg-green-50 border-green-200'
                                            : 'bg-gray-50 border-gray-200'
                                    }`}
                                >
                                    <span className="text-sm">
                                        Uge {result.week} {result.year}
                                    </span>
                                    <div className="flex items-center space-x-2">
                                        <span className="text-sm">
                                            Vindertal:
                                        </span>
                                        <div className="flex space-x-1">
                                            {result.numbers.map((num: number) => (
                                                <div
                                                    key={num}
                                                    className="w-8 h-8 rounded-md bg-[#ed1c24] text-white flex items-center justify-center text-xs"
                                                >
                                                    {num}
                                                </div>
                                            ))}
                                        </div>
                                        {result.winners > 0 && (
                                            <Badge className="bg-green-600 text-white ml-2">
                                                Vindere fundet!
                                            </Badge>
                                        )}
                                    </div>
                                </div>
                            ))
                        )}
                    </div>
                </CardContent>
            </Card>

            {/* Repeat Weeks Dialog */}
            {showRepeatDialog !== null && (
                <Dialog
                    open={true}
                    onOpenChange={() => setShowRepeatDialog(null)}
                >
                    <DialogContent className="max-w-md">
                        <DialogHeader>
                            <DialogTitle>Gentag hver uge</DialogTitle>
                            <DialogDescription>
                                Hvor mange uger i træk vil du spille med dette
                                bræt?
                            </DialogDescription>
                        </DialogHeader>
                        <div className="py-4 space-y-4">
                            <div className="space-y-2">
                                <label className="text-sm">
                                    Antal uger
                                </label>
                                <Input
                                    type="number"
                                    min="1"
                                    max="52"
                                    value={repeatWeeks}
                                    onChange={(e) =>
                                        setRepeatWeeks(e.target.value)
                                    }
                                    placeholder="Indtast antal uger"
                                    className="text-lg"
                                />
                            </div>
                        </div>
                        <DialogFooter>
                            <Button
                                onClick={() => setShowRepeatDialog(null)}
                                className="px-4 py-2 rounded border border-gray-300"
                            >
                                Annuller
                            </Button>
                            <Button
                                className="bg-[#ed1c24] hover:bg-[#d11920] px-4 py-2 rounded text-white"
                                onClick={confirmRepeat}
                            >
                                Bekræft
                            </Button>
                        </DialogFooter>
                    </DialogContent>
                </Dialog>
            )}
        </div>
    );
}
