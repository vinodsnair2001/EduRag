import { Badge } from '@/components/ui/badge'
import type { VectorizationStatus } from '@/types'

const config: Record<VectorizationStatus, { label: string; variant: 'secondary' | 'warning' | 'success' | 'destructive' }> = {
  0: { label: 'Pending',    variant: 'secondary' },
  1: { label: 'Processing', variant: 'warning'   },
  2: { label: 'Completed',  variant: 'success'   },
  3: { label: 'Failed',     variant: 'destructive' },
}

export default function StatusBadge({ status }: { status: VectorizationStatus }) {
  const { label, variant } = config[status]
  return <Badge variant={variant as any}>{label}</Badge>
}
