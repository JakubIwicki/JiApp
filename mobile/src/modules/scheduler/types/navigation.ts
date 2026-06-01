export type SchedulerStackParamList = {
  WeekendGrid: undefined;
  CreateAppointment: { boardId: number };
  AppointmentDetail: { appointmentId: number };
  ClientList: { boardId: number };
  ClientDetail: { clientId: number };
  ServiceList: undefined;
  ServiceEdit: { serviceId?: number; boardId: number };
  Reports: { boardId: number };
  BoardManagement: undefined;
};
