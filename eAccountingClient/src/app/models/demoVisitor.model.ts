export class DemoVisitorModel {
  id: string = '';
  email: string = '';
  isVerified: boolean = false;
  verifiedAt: string | null = null;
  codesSent: number = 0;
  sessionCount: number = 0;
  lastSessionAt: string | null = null;
  firstSeenAt: string = '';
  ipAddress: string | null = null;
  userAgent: string | null = null;
}
