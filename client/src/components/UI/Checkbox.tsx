import type React from "react";

interface CheckboxProps extends React.InputHTMLAttributes<HTMLInputElement> {
    checked?: boolean;
    onCheckedChange?: (checked: boolean) => void;
}

export function Checkbox({ checked, onCheckedChange, className, ...props }: CheckboxProps) {
    return (
        <input
            type="checkbox"
            {...props}
            checked={checked}
            onChange={(e) => onCheckedChange?.(e.target.checked)}
            className={
                "w-4 h-4 rounded border border-gray-400 text-[#ed1c24] " +
                "focus:outline-none focus:ring-2 focus:ring-[#ed1c24] " +
                (className ?? "")
            }
        />
    );
}
