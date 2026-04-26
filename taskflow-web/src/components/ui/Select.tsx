import * as RadixSelect from '@radix-ui/react-select';
import { Check, ChevronDown, ChevronUp } from 'lucide-react';
import { useId } from 'react';
import { cn } from '../../lib/utils';

export interface SelectOption {
  label: string;
  value: string;
}

export interface SelectProps {
  id?: string;
  options: SelectOption[];
  value?: string;
  onChange: (value: string) => void;
  placeholder?: string;
  error?: string;
  className?: string;
  triggerClassName?: string;
}

export function Select({
  id,
  options,
  value,
  onChange,
  placeholder = 'Select an option',
  error,
  className,
  triggerClassName,
}: SelectProps) {
  const autoId = useId();
  const selectId = id ?? autoId;
  const errorId = `${selectId}-error`;

  const triggerClass = cn(
    'flex h-8 w-full items-center justify-between rounded border bg-white px-3 text-13 text-neutral-800 transition-shadow duration-100',
    'focus:outline-none focus:ring-2 focus:ring-primary-200',
    error
      ? 'border-red-400 focus:border-red-400 focus:ring-red-100'
      : 'border-neutral-300 focus:border-primary-500',
  );

  return (
    <div className={cn('w-full', className)}>
      <RadixSelect.Root value={value} onValueChange={onChange}>
        <RadixSelect.Trigger
          id={selectId}
          className={cn(triggerClass, triggerClassName)}
          aria-invalid={Boolean(error)}
          aria-describedby={error ? errorId : undefined}
        >
          <RadixSelect.Value placeholder={placeholder} />
          <RadixSelect.Icon className="text-neutral-400">
            <ChevronDown className="h-3 w-3" />
          </RadixSelect.Icon>
        </RadixSelect.Trigger>

        <RadixSelect.Portal>
          <RadixSelect.Content
            position="popper"
            className="z-50 min-w-[var(--radix-select-trigger-width)] overflow-hidden rounded-md border border-neutral-200 bg-white shadow-e300"
          >
            <RadixSelect.ScrollUpButton className="flex h-6 cursor-default items-center justify-center bg-white text-neutral-500">
              <ChevronUp className="h-3 w-3" />
            </RadixSelect.ScrollUpButton>
            <RadixSelect.Viewport className="p-0">
              {options.map((option) => (
                <RadixSelect.Item
                  key={option.value}
                  value={option.value}
                  className={cn(
                    'relative flex h-8 cursor-pointer select-none items-center pl-8 pr-3 text-13 text-neutral-800 outline-none',
                    'data-[highlighted]:bg-neutral-50',
                    'data-[state=checked]:bg-primary-50 data-[state=checked]:font-medium data-[state=checked]:text-primary-700',
                  )}
                >
                  <RadixSelect.ItemIndicator className="absolute left-2 flex h-3 w-3 items-center justify-center">
                    <Check className="h-3 w-3 text-primary-600" />
                  </RadixSelect.ItemIndicator>
                  <RadixSelect.ItemText>{option.label}</RadixSelect.ItemText>
                </RadixSelect.Item>
              ))}
            </RadixSelect.Viewport>
            <RadixSelect.ScrollDownButton className="flex h-6 cursor-default items-center justify-center bg-white text-neutral-500">
              <ChevronDown className="h-3 w-3" />
            </RadixSelect.ScrollDownButton>
          </RadixSelect.Content>
        </RadixSelect.Portal>
      </RadixSelect.Root>

      {error ? (
        <p id={errorId} className="mt-1 text-12 text-red-600">
          {error}
        </p>
      ) : null}
    </div>
  );
}
