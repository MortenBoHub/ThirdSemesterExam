import type React from "react";

interface NumberBoardProps {
    selectedNumbers: number[];
    onNumberSelect: (num: number) => void;
}

export default function NumberBoard({ selectedNumbers, onNumberSelect }: NumberBoardProps) {
    const numbers = Array.from({ length: 16 }, (_, i) => i + 1);

    return (
        <div className="grid grid-cols-4 gap-2">
            {numbers.map((num) => {
                const isSelected = selectedNumbers.includes(num);

                return (
                    <button
                        key={num}
                        type="button"
                        onClick={() => onNumberSelect(num)}
                        className={
                            "w-24 h-24 rounded-md text-sm font-medium flex items-center justify-center transition " +
                            (isSelected
                                ? "bg-[#ed1c24] text-white shadow"
                                : "bg-gray-100 text-gray-800 hover:bg-gray-200")
                        }
                    >
                        {num}
                    </button>
                );
            })}
        </div>
    );
}