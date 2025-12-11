import * as React from "react";
import { Slot } from "@radix-ui/react-slot";
import { cva, type VariantProps } from "class-variance-authority";

import { cn } from "./utils";

const badgeVariants = cva(
    "inline-flex items-center justify-center rounded-md border px-2 py-0.5 text-xs font-medium w-fit whitespace-nowrap shrink-0 [&>svg]:size-3 gap-1 [&>svg]:pointer-events-none transition-colors overflow-hidden",
    {
        variants: {
            variant: {
                default:
                    "bg-[#ed1c24] text-white border-transparent",
                secondary:
                    "bg-gray-200 text-gray-800 border-transparent",
                destructive:
                    "bg-red-600 text-white border-transparent",
                outline:
                    "border-gray-400 text-gray-800",
            },
        },
        defaultVariants: {
            variant: "default",
        },
    }
);

export function Badge({
                          className,
                          variant,
                          asChild = false,
                          ...props
                      }: React.ComponentProps<"span"> &
    VariantProps<typeof badgeVariants> & {
    asChild?: boolean;
})
{
    const Comp = asChild ? Slot : "span";

    return (
        <Comp
            className={cn(badgeVariants({ variant }), className)}
            {...props}
        />
    );
}

export { badgeVariants };
