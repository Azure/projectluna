import uuid from "uuid";
import * as yup from "yup";
import { ObjectSchema } from "yup";
import { ISubscriptionsPostModel, ISubscriptionsModel, ISubscriptionsWarnings } from "../../../models";
import { IWizardModel } from "../../../models/IWizardModel";
import { ErrorMessage } from "../../Wizard/formUtils/ErrorMessage";
import { httpURLRegExp, opetarionNameRegExp, versionNameRegExp } from "../../Products/formUtils/RegExp";

export interface IWizardFormValues {
  wizard: IWizardModel;
}

const wizardFormValidator: ObjectSchema<IWizardModel> = yup.object().shape(
  {
  sourceServiceType: yup.string().required("Source Service Type required"),
  sourceService: yup.string().required("Source Service required"),
  mLComponentType: yup.string().required("ML Component Type required"),
  mLComponent: yup.string().required("ML Component required"),
  operationName: yup.string().matches(opetarionNameRegExp,
    {
      message: ErrorMessage.operationName,
      excludeEmptyString: true
    }).required("Operation Name is required"),
    // .max(128,"The Operation Name is too long. Must be no more than 128 characters"),
  applicationDisplayName: yup.string().matches(opetarionNameRegExp,
    {
      message: ErrorMessage.applicationDisplayName,
      excludeEmptyString: true
    }).required("Application Display Name is required"),
  applicationName: yup.string().matches(opetarionNameRegExp,
    {
      message: ErrorMessage.applicationName,
      excludeEmptyString: true
    }).required("Application Name is required"),
  apiName: yup.string().matches(opetarionNameRegExp,
    {
      message: ErrorMessage.apiName,
      excludeEmptyString: true
    }).required("API Name is required"),
  apiVersion: yup.string().matches(versionNameRegExp,
    {
      message: ErrorMessage.apiVersion,
      excludeEmptyString: true
    }).required("Version is required"),
  applicationDescription: yup.string(),
  logoImageURL: yup.string().matches(httpURLRegExp,
    {
      message: ErrorMessage.Url,
      excludeEmptyString: true
    }),
  documentationURL: yup.string().matches(httpURLRegExp,
    {
      message: ErrorMessage.Url,
      excludeEmptyString: true
    }),
  // publisher: yup.string().matches(opetarionNameRegExp,
  //   {
  //     message: ErrorMessage.publisher,
  //     excludeEmptyString: true
  //   }),
  publisher: yup.string(),
  branchOrCommitHash: yup.string(),
  executionConfigFile: yup.string(),
  computeServiceType: yup.string(),
  computeService: yup.string(),
  clientId: yup.string()
  }
);

export const wizardFormValidationSchema: ObjectSchema<IWizardFormValues> =
  yup.object().shape({
    wizard: wizardFormValidator
  });

  export const initialWizardValues: IWizardModel = {
    sourceServiceType: '',
    sourceService: '',
    mLComponentType: '',
    mLComponent: '',
    operationName: '',
    applicationDisplayName: '',
    applicationName: '',
    apiName: '',
    apiVersion: '',
    applicationDescription: '',
    logoImageURL: '',
    documentationURL: '',
    publisher: '',
    branchOrCommitHash: '',
    executionConfigFile: '',
    computeServiceType: '',
    computeService: '',
    clientId: uuid()
  };

  export const initialWizardFormValues: IWizardFormValues = {
    wizard: initialWizardValues
  }

  

  