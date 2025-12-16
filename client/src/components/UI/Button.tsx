import React from 'react';

interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
    children: React.ReactNode;
}

export const Button: React.FC<ButtonProps> = ({ children, ...props }) => {
    return (
        <button {...props} className={`px-10 py-5.5 rounded ${props.className}`}>
            {children}
        </button>
    );
};