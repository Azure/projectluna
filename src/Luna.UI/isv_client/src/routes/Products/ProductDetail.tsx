import React, { useEffect, useState } from 'react';
import {
  Stack
} from 'office-ui-fabric-react';
import { useParams } from 'react-router-dom';

import ProductDeployments from '../Products/Deployments';
import AMLWorkSpace from '../Products/AMLWorkSpace'
import ProductService from '../../services/ProductService';


const ProductDetail: React.FunctionComponent = () => {

  const { applicationName } = useParams();
  const [description, setDescription] = useState<string>("");
  const [loading, setloading] = useState<boolean>(true);

  const getProduct = async () => {
    setloading(true);
    const results = await ProductService.get(applicationName as string);
    if (results && !results.hasErrors && results.value)
      setDescription(results.value.description);
    else {
      if (results.hasErrors) {
        // TODO: display errors
        alert(results.errors.join(', '));
      }
    }

    setloading(false);
  }
  useEffect(() => {
    getProduct();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return (
    <Stack
      horizontalAlign="center"
      verticalAlign="start"
      verticalFill
      styles={{
        root: {
          width: '100%',
          margin: '0 auto',
          textAlign: 'center',
          color: '#605e5c'
        }
      }}
      gap={15}
    >
      {!loading ?
        <React.Fragment>
          <ProductDeployments productType={"SaaS"} />
        </React.Fragment> : null
      }

    </Stack>
  );

}

export default ProductDetail;