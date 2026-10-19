import { forwardRef, type ComponentPropsWithoutRef } from 'react';

// Native equivalents for upstream shadcn wrappers; scrolling uses the browser.
export function cn(...values: (string | undefined | Record<string, boolean>)[]) {
  return values.flatMap(value => typeof value === 'string' ? [value] : value ? Object.keys(value).filter(key => value[key]) : []).join(' ');
}
export const ScrollArea = forwardRef<HTMLDivElement, ComponentPropsWithoutRef<'div'>>((props, ref) => <div {...props} ref={ref}/>);
export const Button = forwardRef<HTMLButtonElement, ComponentPropsWithoutRef<'button'> & { variant?: string }>(({ variant: _variant, ...props }, ref) => <button type="button" {...props} ref={ref}/>);
